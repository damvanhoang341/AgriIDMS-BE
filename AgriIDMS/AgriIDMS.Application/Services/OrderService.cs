using AgriIDMS.Application.DTOs.Order;
using AgriIDMS.Application.Exceptions;
using AgriIDMS.Application.Interfaces;
using AgriIDMS.Domain;
using AgriIDMS.Domain.Entities;
using AgriIDMS.Domain.Enums;
using AgriIDMS.Domain.Exceptions;
using AgriIDMS.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
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
        private readonly IInventoryTransactionRepository _inventoryTranRepo;
        private readonly IUnitOfWork _uow;
        private readonly ILogger<OrderService> _logger;

        private const decimal DefaultColdStorageHours = 48m;
        private const int AllocationExpirationHours = 24;

        public OrderService(
            ICartRepository cartRepo,
            IOrderRepository orderRepo,
            IBoxRepository boxRepo,
            IOrderAllocationRepository allocationRepo,
            IInventoryTransactionRepository inventoryTranRepo,
            IUnitOfWork uow,
            ILogger<OrderService> logger)
        {
            _cartRepo = cartRepo;
            _orderRepo = orderRepo;
            _boxRepo = boxRepo;
            _allocationRepo = allocationRepo;
            _inventoryTranRepo = inventoryTranRepo;
            _uow = uow;
            _logger = logger;
        }

        public async Task<CreateOrderFromCartResponse> CreateOrderFromCartAsync(string userId)
        {
            _logger.LogInformation("Creating order from cart for user {UserId}", userId);

            var cart = await _cartRepo.GetByUserIdWithItemsAsync(userId);
            if (cart == null || cart.Items == null || !cart.Items.Any())
                throw new InvalidBusinessRuleException("Giỏ hàng trống");

            var now = DateTime.UtcNow;

            await _uow.BeginTransactionAsync();
            try
            {
                decimal estimatedTotal = 0;
                var order = new Order
                {
                    UserId = userId,
                    CreatedAt = now,
                    Status = OrderStatus.AwaitingPayment
                };

                foreach (var item in cart.Items)
                {
                    var detail = new OrderDetail
                    {
                        ProductVariantId = item.ProductVariantId,
                        Quantity = (int)item.Quantity,
                        UnitPrice = item.UnitPrice,
                        FulfilledQuantity = 0,
                        ShortageQuantity = 0
                    };
                    order.Details.Add(detail);
                    estimatedTotal += detail.Quantity * detail.UnitPrice;
                }

                order.TotalAmount = estimatedTotal;

                var items = cart.Items.Select(i => new OrderItemDto
                {
                    ProductVariantId = i.ProductVariantId,
                    ProductName = i.ProductVariant?.Product?.Name ?? string.Empty,
                    Grade = i.ProductVariant?.Grade.ToString() ?? string.Empty,
                    Quantity = (int)i.Quantity,
                    UnitPrice = i.UnitPrice
                }).ToList();

                await _orderRepo.AddAsync(order);
                await _cartRepo.ClearCartAsync(cart);
                await _uow.CommitAsync();

                _logger.LogInformation(
                    "Order {OrderId} created from cart for user {UserId}, estimated total {Total}",
                    order.Id, userId, estimatedTotal);

                return new CreateOrderFromCartResponse
                {
                    OrderId = order.Id,
                    TotalAmount = estimatedTotal,
                    Items = items
                };
            }
            catch
            {
                await _uow.RollbackAsync();
                throw;
            }
        }

        public async Task<CreateOrderFromCartResponse> CreateOrderFromCartByVariantIdsAsync(
            string userId,
            IList<int> productVariantIds)
        {
            _logger.LogInformation(
                "Creating order from cart variants ({ProductVariantIdsCount} ids) for user {UserId}",
                productVariantIds?.Count ?? 0, userId);

            if (productVariantIds == null || !productVariantIds.Any())
                throw new InvalidBusinessRuleException("Bạn phải chọn ít nhất 1 loại sản phẩm");

            var cart = await _cartRepo.GetByUserIdWithItemsAsync(userId);
            if (cart == null || cart.Items == null || !cart.Items.Any())
                throw new InvalidBusinessRuleException("Giỏ hàng trống");

            var variantIdSet = productVariantIds.ToHashSet();
            var selectedItems = cart.Items
                .Where(i => variantIdSet.Contains(i.ProductVariantId))
                .ToList();

            if (!selectedItems.Any())
                throw new InvalidBusinessRuleException("Trong giỏ hàng không có các loại sản phẩm được chọn");

            var now = DateTime.UtcNow;
            await _uow.BeginTransactionAsync();
            try
            {
                decimal estimatedTotal = 0;
                var order = new Order
                {
                    UserId = userId,
                    CreatedAt = now,
                    Status = OrderStatus.AwaitingPayment
                };

                foreach (var item in selectedItems)
                {
                    var detail = new OrderDetail
                    {
                        ProductVariantId = item.ProductVariantId,
                        Quantity = (int)item.Quantity,
                        UnitPrice = item.UnitPrice,
                        FulfilledQuantity = 0,
                        ShortageQuantity = 0
                    };

                    order.Details.Add(detail);
                    estimatedTotal += detail.Quantity * detail.UnitPrice;
                }

                order.TotalAmount = estimatedTotal;

                var items = selectedItems.Select(i => new OrderItemDto
                {
                    ProductVariantId = i.ProductVariantId,
                    ProductName = i.ProductVariant?.Product?.Name ?? string.Empty,
                    Grade = i.ProductVariant?.Grade.ToString() ?? string.Empty,
                    Quantity = (int)i.Quantity,
                    UnitPrice = i.UnitPrice
                }).ToList();

                await _orderRepo.AddAsync(order);

                // Chỉ xóa các item thuộc các ProductVariantId đã chọn khỏi cart.
                foreach (var item in selectedItems)
                    _cartRepo.RemoveItem(item);

                cart.UpdatedAt = DateTime.UtcNow;
                await _uow.CommitAsync();

                return new CreateOrderFromCartResponse
                {
                    OrderId = order.Id,
                    TotalAmount = estimatedTotal,
                    Items = items
                };
            }
            catch
            {
                await _uow.RollbackAsync();
                throw;
            }
        }

        public async Task AllocateInventoryAsync(int orderId, string userId)
        {
            _logger.LogInformation("Allocating inventory for order {OrderId} by user {UserId}", orderId, userId);

            var order = await _orderRepo.GetByIdWithDetailsAsync(orderId)
                ?? throw new NotFoundException($"Order #{orderId} không tồn tại");

            if (order.UserId != userId)
                throw new ForbiddenException("Bạn không có quyền thao tác trên đơn hàng này");

            if (order.Status != OrderStatus.AwaitingPayment)
                throw new InvalidBusinessRuleException(
                    $"Chỉ có thể allocate đơn hàng ở trạng thái AwaitingPayment. Hiện tại: {order.Status}");

            if (order.Details == null || !order.Details.Any())
                throw new InvalidBusinessRuleException("Order không có chi tiết");

            await _uow.BeginTransactionAsync();
            try
            {
                var now = DateTime.UtcNow;
                var expiredAt = now.AddHours(AllocationExpirationHours);
                var allAllocations = new List<OrderAllocation>();
                var allTransactions = new List<InventoryTransaction>();
                decimal totalAmount = 0;
                var coldStorageWarnings = new List<string>();

                foreach (var detail in order.Details)
                {
                    var boxesNeeded = (int)detail.Quantity;
                    var boxes = await _boxRepo.GetAvailableBoxesForVariantAsync(detail.ProductVariantId);

                    var allocated = 0;
                    foreach (var box in boxes)
                    {
                        if (allocated >= boxesNeeded) break;

                        if (!IsColdStorageEligible(box, coldStorageWarnings))
                            continue;

                        allAllocations.Add(new OrderAllocation
                        {
                            OrderId = order.Id,
                            OrderDetailId = detail.Id,
                            BoxId = box.Id,
                            ReservedQuantity = box.Weight,
                            Status = AllocationStatus.Reserved,
                            ReservedAt = now,
                            ExpiredAt = expiredAt
                        });

                        allTransactions.Add(new InventoryTransaction
                        {
                            BoxId = box.Id,
                            TransactionType = InventoryTransactionType.Export,
                            FromSlotId = box.SlotId,
                            ToSlotId = null,
                            Quantity = box.Weight,
                            ReferenceType = ReferenceType.GoodsIssue,
                            CreatedBy = userId,
                            CreatedAt = now
                        });

                        box.Status = BoxStatus.Reserved;
                        await _boxRepo.UpdateAsync(box);

                        totalAmount += box.Weight * detail.UnitPrice;
                        allocated++;
                    }

                    detail.FulfilledQuantity = allocated;
                    detail.ShortageQuantity = boxesNeeded - allocated;
                }

                var allFulfilled = order.Details.All(d => d.ShortageQuantity == 0);

                if (!allFulfilled)
                {
                    foreach (var alloc in allAllocations)
                    {
                        var box = await _boxRepo.GetByIdAsync(alloc.BoxId);
                        if (box != null)
                        {
                            box.Status = BoxStatus.Stored;
                            await _boxRepo.UpdateAsync(box);
                        }
                    }

                    order.Status = OrderStatus.InventoryFailed;
                    await _uow.CommitAsync();

                    var shortageDetails = order.Details
                        .Where(d => d.ShortageQuantity > 0)
                        .Select(d => $"Variant #{d.ProductVariantId}: thiếu {d.ShortageQuantity} box");

                    _logger.LogWarning(
                        "Inventory allocation failed for order {OrderId}. Shortage: {Details}",
                        orderId, string.Join(", ", shortageDetails));

                    throw new InvalidBusinessRuleException(
                        $"Không đủ hàng tồn kho để giữ cho đơn hàng. {string.Join("; ", shortageDetails)}");
                }

                await _allocationRepo.AddRangeAsync(allAllocations);
                await _inventoryTranRepo.AddRangeAsync(allTransactions);

                order.TotalAmount = totalAmount;
                order.Status = OrderStatus.Confirmed;

                await _uow.CommitAsync();

                _logger.LogInformation(
                    "Inventory allocated for order {OrderId}: {BoxCount} boxes, total {Total}, status → Confirmed",
                    orderId, allAllocations.Count, totalAmount);
            }
            catch (InvalidBusinessRuleException)
            {
                throw;
            }
            catch
            {
                await _uow.RollbackAsync();
                throw;
            }
        }

        /// <summary>
        /// Kiểm tra box có đủ điều kiện xuất kho lạnh không.
        /// Box ở kho thường luôn được phép. Box ở kho lạnh phải đủ MinColdStorageHours.
        /// </summary>
        private bool IsColdStorageEligible(Box box, List<string> warnings)
        {
            var warehouse = box.Slot?.Rack?.Zone?.Warehouse;
            if (warehouse == null || warehouse.TitleWarehouse != TitleWarehouse.Cold)
                return true;

            var minHours = warehouse.MinColdStorageHours ?? DefaultColdStorageHours;
            if (ColdStorageExportRule.CanExportFromCold(box.PlacedInColdAt, minHours))
                return true;

            var message = ColdStorageExportRule.GetNotEligibleMessage(box.BoxCode, minHours, box.PlacedInColdAt);
            warnings.Add(message);
            _logger.LogInformation("Cold storage skip: {Message}", message);
            return false;
        }
    }
}
