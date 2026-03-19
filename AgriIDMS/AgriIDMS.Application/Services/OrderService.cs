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

        public async Task<IList<OrderListItemDto>> GetMyOrdersAsync(string userId, GetOrdersQuery query)
        {
            var orders = await _orderRepo.GetByUserIdWithDetailsAndPaymentsAsync(userId);

            if (!string.IsNullOrWhiteSpace(query?.Status)
                && Enum.TryParse<OrderStatus>(query.Status, true, out var parsedStatus))
            {
                orders = orders.Where(o => o.Status == parsedStatus).ToList();
            }

            return orders.Select(o => new OrderListItemDto
            {
                OrderId = o.Id,
                TotalAmount = o.TotalAmount,
                Status = o.Status.ToString(),
                CreatedAt = o.CreatedAt,
                ItemCount = o.Details?.Count ?? 0,
                LatestPaymentStatus = o.Payments?
                    .OrderByDescending(p => p.CreatedAt)
                    .FirstOrDefault()?
                    .PaymentStatus
                    .ToString()
            }).ToList();
        }

        public async Task<OrderDetailDto> GetMyOrderByIdAsync(int orderId, string userId)
        {
            var order = await _orderRepo.GetByIdWithDetailsAndPaymentsAsync(orderId)
                ?? throw new NotFoundException($"Order #{orderId} không tồn tại");

            if (order.UserId != userId)
                throw new ForbiddenException("Bạn không có quyền xem đơn hàng này");

            return new OrderDetailDto
            {
                OrderId = order.Id,
                TotalAmount = order.TotalAmount,
                Status = order.Status.ToString(),
                CreatedAt = order.CreatedAt,
                LatestPaymentStatus = order.Payments?
                    .OrderByDescending(p => p.CreatedAt)
                    .FirstOrDefault()?
                    .PaymentStatus
                    .ToString(),
                Items = order.Details.Select(d => new OrderDetailItemDto
                {
                    ProductVariantId = d.ProductVariantId,
                    ProductName = d.ProductVariant?.Product?.Name ?? string.Empty,
                    Grade = d.ProductVariant?.Grade.ToString() ?? string.Empty,
                    BoxWeight = d.BoxWeight,
                    IsPartial = d.IsPartial,
                    Quantity = d.Quantity,
                    UnitPrice = d.UnitPrice,
                    FulfilledQuantity = d.FulfilledQuantity,
                    ShortageQuantity = d.ShortageQuantity
                }).ToList()
            };
        }

        public async Task CancelOrderAsync(int orderId, string userId)
        {
            var order = await _orderRepo.GetByIdWithDetailsAndPaymentsAsync(orderId)
                ?? throw new NotFoundException($"Order #{orderId} không tồn tại");

            if (order.UserId != userId)
                throw new ForbiddenException("Bạn không có quyền hủy đơn hàng này");

            // "trước khi vào allocate" nghĩa là chưa được giữ hàng.
            // Trong model hiện tại, allocation chỉ xuất hiện khi Order đã chuyển khỏi AwaitingPayment.
            if (order.Status != OrderStatus.AwaitingPayment && order.Status != OrderStatus.InventoryFailed)
                throw new InvalidBusinessRuleException(
                    $"Chỉ có thể hủy đơn khi đơn chưa được allocate/giữ hàng. Hiện tại: {order.Status}");

            if (order.Payments != null && order.Payments.Any(p =>
                    p.PaymentStatus == PaymentStatus.Success ||
                    p.PaymentStatus == PaymentStatus.Refunded))
            {
                throw new InvalidBusinessRuleException("Không thể hủy đơn: đơn đã thanh toán thành công");
            }

            await _uow.BeginTransactionAsync();
            try
            {
                // Nếu có payment đang Pending/Processing thì chuyển sang Cancelled (tránh dữ liệu treo).
                if (order.Payments != null)
                {
                    foreach (var p in order.Payments)
                    {
                        if (p.PaymentStatus == PaymentStatus.Pending ||
                            p.PaymentStatus == PaymentStatus.Processing)
                        {
                            p.PaymentStatus = PaymentStatus.Cancelled;
                        }
                    }
                }

                // Revert các allocation/box nếu có (dự phòng).
                var allocations = await _allocationRepo.GetByOrderIdAsync(orderId);
                if (allocations != null && allocations.Any())
                {
                    foreach (var alloc in allocations)
                    {
                        if (alloc.Status == AllocationStatus.Reserved || alloc.Status == AllocationStatus.Picked)
                        {
                            alloc.Status = AllocationStatus.Cancelled;

                            if (alloc.Box != null &&
                                (alloc.Box.Status == BoxStatus.Reserved ||
                                 alloc.Box.Status == BoxStatus.Picking))
                            {
                                alloc.Box.Status = BoxStatus.Stored;
                            }
                        }
                    }
                }

                order.Status = OrderStatus.Cancelled;

                await _uow.CommitAsync();
            }
            catch
            {
                await _uow.RollbackAsync();
                throw;
            }
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
                        BoxWeight = item.BoxWeight,
                        IsPartial = item.IsPartial,
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
                    BoxWeight = i.BoxWeight,
                    IsPartial = i.IsPartial,
                    Quantity = (int)i.Quantity,
                    UnitPrice = i.UnitPrice
                }).ToList();

                await _orderRepo.AddAsync(order);
                await _cartRepo.ClearCartAsync(cart);
                await _uow.CommitAsync();

                _logger.LogInformation(
                    "Order {OrderId} created from cart for user {UserId}, estimated total {Total}",
                    order.Id, userId, estimatedTotal);

                var response = new CreateOrderFromCartResponse
                {
                    OrderId = order.Id,
                    TotalAmount = estimatedTotal,
                    Items = items
                };
                await TryAutoAllocateAsync(order.Id, userId, response);
                return response;
            }
            catch
            {
                await _uow.RollbackAsync();
                throw;
            }
        }

        public async Task<CreateOrderFromCartResponse> CreateOrderFromCartByVariantIdsAsync(string userId,IList<CreateOrderFromCartByVariantIdsRequest> requestItems)
        {
            _logger.LogInformation(
                "Creating order from cart variants ({Count}) for user {UserId}",
                requestItems?.Count ?? 0, userId);

            if (requestItems == null || !requestItems.Any())
                throw new InvalidBusinessRuleException("Bạn phải chọn ít nhất 1 loại sản phẩm");

            var cart = await _cartRepo.GetByUserIdWithItemsAsync(userId);
            if (cart == null || cart.Items == null || !cart.Items.Any())
                throw new InvalidBusinessRuleException("Giỏ hàng trống");

            // Request chỉ truyền theo ProductVariantId, trong cart có thể có nhiều CartItem khác nhau
            // (phân biệt IsPartial/BoxWeight). Do đó không được apply cùng Quantity cho tất cả CartItem.
            // Ta phân bổ tuần tự theo từng CartItem cho tới khi đủ tổng quantity theo request.
            var requestDict = requestItems
                .GroupBy(x => new { x.ProductVariantId, x.BoxWeight, x.IsPartial })
                .ToDictionary(
                    g => (g.Key.ProductVariantId, g.Key.BoxWeight, g.Key.IsPartial),
                    g => g.Sum(x => x.Quantity));

            var selectedItems = cart.Items
                .Where(i => requestDict.Keys.Any(k =>
                    k.ProductVariantId == i.ProductVariantId &&
                    k.BoxWeight == i.BoxWeight &&
                    k.IsPartial == i.IsPartial))
                .ToList();

            if (!selectedItems.Any())
                throw new InvalidBusinessRuleException("Không tìm thấy sản phẩm trong giỏ");

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

                var orderItems = new List<OrderItemDto>();

                foreach (var variantRequest in requestDict)
                {
                    var productVariantId = variantRequest.Key.ProductVariantId;
                    var boxWeight = variantRequest.Key.BoxWeight;
                    var isPartial = variantRequest.Key.IsPartial;
                    var requestedTotalQty = variantRequest.Value;

                    if (requestedTotalQty <= 0)
                        throw new InvalidBusinessRuleException("Số lượng phải lớn hơn 0");

                    var cartItemsForVariant = selectedItems
                        .Where(i =>
                            i.ProductVariantId == productVariantId &&
                            i.BoxWeight == boxWeight &&
                            i.IsPartial == isPartial)
                        .ToList();

                    if (!cartItemsForVariant.Any())
                        throw new InvalidBusinessRuleException("Không tìm thấy sản phẩm trong giỏ");

                    // CartService đang đảm bảo unique theo (VariantId, IsPartial, BoxWeight)
                    // Nếu vì lý do nào đó có nhiều dòng, vẫn handle bằng cách cộng tổng.
                    var availableTotalQty = cartItemsForVariant.Sum(i => (int)i.Quantity);
                    if (requestedTotalQty > availableTotalQty)
                        throw new InvalidBusinessRuleException("Số lượng vượt quá trong giỏ hàng");

                    var remainingQty = requestedTotalQty;

                    foreach (var item in cartItemsForVariant)
                    {
                        if (remainingQty <= 0) break;

                        var availableQty = (int)item.Quantity;
                        if (availableQty <= 0) continue;

                        var qtyToTake = Math.Min(availableQty, remainingQty);

                        var detail = new OrderDetail
                        {
                            ProductVariantId = item.ProductVariantId,
                            BoxWeight = item.BoxWeight,
                            IsPartial = item.IsPartial,
                            Quantity = qtyToTake,
                            UnitPrice = item.UnitPrice,
                            FulfilledQuantity = 0,
                            ShortageQuantity = 0
                        };

                        order.Details.Add(detail);
                        estimatedTotal += detail.Quantity * detail.UnitPrice;

                        orderItems.Add(new OrderItemDto
                        {
                            ProductVariantId = item.ProductVariantId,
                            ProductName = item.ProductVariant?.Product?.Name ?? string.Empty,
                            Grade = item.ProductVariant?.Grade.ToString() ?? string.Empty,
                            BoxWeight = item.BoxWeight,
                            IsPartial = item.IsPartial,
                            Quantity = qtyToTake,
                            UnitPrice = item.UnitPrice
                        });

                        // Cập nhật lại cart (không apply cùng quantity cho tất cả CartItem)
                        if (qtyToTake == availableQty)
                        {
                            _cartRepo.RemoveItem(item);
                        }
                        else
                        {
                            item.Quantity -= qtyToTake;
                        }

                        remainingQty -= qtyToTake;
                    }
                }

                order.TotalAmount = estimatedTotal;

                await _orderRepo.AddAsync(order);

                cart.UpdatedAt = DateTime.UtcNow;

                await _uow.CommitAsync();

                var response = new CreateOrderFromCartResponse
                {
                    OrderId = order.Id,
                    TotalAmount = estimatedTotal,
                    Items = orderItems
                };
                await TryAutoAllocateAsync(order.Id, userId, response);
                return response;
            }
            catch
            {
                await _uow.RollbackAsync();
                throw;
            }
        }

        /// <summary>Gọi allocate ngay sau khi tạo order. Nếu thất bại (thiếu hàng) vẫn trả về response, không ném lỗi.</summary>
        private async Task TryAutoAllocateAsync(int orderId, string userId, CreateOrderFromCartResponse response)
        {
            try
            {
                await AllocateInventoryAsync(orderId, userId);
                response.AllocationSucceeded = true;
                _logger.LogInformation("Auto-allocate succeeded for order {OrderId}", orderId);
            }
            catch (Exception ex)
            {
                response.AllocationSucceeded = false;
                response.AllocationMessage = ex.Message;
                _logger.LogWarning(ex, "Auto-allocate failed for order {OrderId}", orderId);
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
                    // Enforce đúng loại box theo order detail
                    boxes = boxes
                        .Where(b => b.IsPartial == detail.IsPartial && b.Weight == detail.BoxWeight)
                        .ToList();

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
