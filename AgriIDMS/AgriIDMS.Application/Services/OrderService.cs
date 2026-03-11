using AgriIDMS.Application.Interfaces;
using AgriIDMS.Domain.Entities;
using AgriIDMS.Domain.Enums;
using AgriIDMS.Domain.Exceptions;
using AgriIDMS.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace AgriIDMS.Application.Services
{
    public class OrderService : IOrderService
    {
        private readonly ICartRepository _cartRepo;
        private readonly IOrderRepository _orderRepo;
        private readonly IBoxRepository _boxRepo;
        private readonly IOrderAllocationRepository _allocationRepo;
        private readonly IUnitOfWork _uow;
        private readonly ILogger<OrderService> _logger;

        public OrderService(
            ICartRepository cartRepo,
            IOrderRepository orderRepo,
            IBoxRepository boxRepo,
            IOrderAllocationRepository allocationRepo,
            IUnitOfWork uow,
            ILogger<OrderService> logger)
        {
            _cartRepo = cartRepo;
            _orderRepo = orderRepo;
            _boxRepo = boxRepo;
            _allocationRepo = allocationRepo;
            _uow = uow;
            _logger = logger;
        }

        public async Task<int> CreateOrderFromCartAsync(string userId)
        {
            _logger.LogInformation("Creating order from cart for user {UserId}", userId);

            var cart = await _cartRepo.GetByUserIdWithItemsAsync(userId);
            if (cart == null || cart.Items == null || !cart.Items.Any())
                throw new InvalidBusinessRuleException("Giỏ hàng trống");

            var now = DateTime.UtcNow;

            await _uow.BeginTransactionAsync();
            try
            {
                var order = new Order
                {
                    UserId = userId,
                    CreatedAt = now,
                    TotalAmount = 0,
                    Status = OrderStatus.AwaitingPayment
                };

                foreach (var item in cart.Items)
                {
                    order.Details.Add(new OrderDetail
                    {
                        ProductVariantId = item.ProductVariantId,
                        Quantity = (int)item.Quantity,
                        UnitPrice = item.UnitPrice,
                        FulfilledQuantity = 0,
                        ShortageQuantity = 0
                    });
                }

                await _orderRepo.AddAsync(order);
                await _cartRepo.ClearCartAsync(cart);
                await _uow.SaveChangesAsync();
                await _uow.CommitAsync();

                _logger.LogInformation("Order {OrderId} created from cart for user {UserId}", order.Id, userId);
                return order.Id;
            }
            catch
            {
                await _uow.RollbackAsync();
                throw;
            }
        }

        public async Task AllocateInventoryAsync(int orderId)
        {
            _logger.LogInformation("Allocating inventory for order {OrderId}", orderId);

            var order = await _orderRepo.GetByIdWithDetailsAsync(orderId);
            if (order == null)
                throw new InvalidBusinessRuleException("Order không tồn tại");

            if (order.Status != OrderStatus.AwaitingPayment)
                throw new InvalidBusinessRuleException("Chỉ có thể allocate đơn hàng đang ở trạng thái AwaitingPayment");

            if (order.Details == null || !order.Details.Any())
                throw new InvalidBusinessRuleException("Order không có chi tiết");

            await _uow.BeginTransactionAsync();
            try
            {
                var allAllocations = new System.Collections.Generic.List<OrderAllocation>();
                decimal totalAmount = 0;

                foreach (var detail in order.Details)
                {
                    var boxesNeeded = (int)detail.Quantity;
                    var boxes = await _boxRepo.GetAvailableBoxesForVariantAsync(detail.ProductVariantId);

                    var allocated = 0;
                    foreach (var box in boxes)
                    {
                        if (allocated >= boxesNeeded) break;

                        allAllocations.Add(new OrderAllocation
                        {
                            OrderId = order.Id,
                            OrderDetailId = detail.Id,
                            BoxId = box.Id,
                            ReservedQuantity = box.Weight,
                            Status = AllocationStatus.Reserved,
                            ReservedAt = DateTime.UtcNow
                        });

                        box.Status = BoxStatus.Reserved;
                        await _boxRepo.UpdateAsync(box);

                        totalAmount += box.Weight * detail.UnitPrice;
                        allocated++;
                    }

                    detail.FulfilledQuantity = allocated;
                    detail.ShortageQuantity = boxesNeeded - allocated;
                }

                if (allAllocations.Any())
                {
                    await _allocationRepo.AddRangeAsync(allAllocations);
                }

                order.TotalAmount = totalAmount;

                if (order.Details.All(d => d.ShortageQuantity == 0))
                    order.Status = OrderStatus.Confirmed;
                else
                    order.Status = OrderStatus.InventoryFailed;

                await _uow.SaveChangesAsync();
                await _uow.CommitAsync();

                _logger.LogInformation("Allocated inventory for order {OrderId} with status {Status}", orderId, order.Status);
            }
            catch
            {
                await _uow.RollbackAsync();
                throw;
            }
        }
    }
}

