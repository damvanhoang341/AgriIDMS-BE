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

            // Chưa giữ hàng: chờ sale, chờ allocate, hoặc allocate thất bại (chưa thanh toán).
            var canCancelBeforeAllocation =
                order.Status == OrderStatus.PendingSaleConfirmation
                || order.Status == OrderStatus.AwaitingAllocation
                || order.Status == OrderStatus.PartiallyAllocated
                || order.Status == OrderStatus.AwaitingPayment
                || order.Status == OrderStatus.BackorderWaiting
                || order.Status == OrderStatus.InventoryFailed;

            if (!canCancelBeforeAllocation)
                throw new InvalidBusinessRuleException(
                    $"Chỉ có thể hủy đơn khi chưa giữ hàng / chưa thanh toán thành công. Hiện tại: {order.Status}");

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
                    Status = OrderStatus.PendingSaleConfirmation
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

                return new CreateOrderFromCartResponse
                {
                    OrderId = order.Id,
                    TotalAmount = estimatedTotal,
                    Items = items,
                    AllocationSucceeded = false,
                    AllocationMessage = null
                };
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
                    Status = OrderStatus.PendingSaleConfirmation
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

                return new CreateOrderFromCartResponse
                {
                    OrderId = order.Id,
                    TotalAmount = estimatedTotal,
                    Items = orderItems,
                    AllocationSucceeded = false,
                    AllocationMessage = null
                };
            }
            catch
            {
                await _uow.RollbackAsync();
                throw;
            }
        }

        /// <summary>Sale xác nhận đơn → đơn chuyển sang chờ giữ hàng (allocate).</summary>
        public async Task SaleConfirmOrderAsync(int orderId, string confirmedByUserId)
        {
            var order = await _orderRepo.GetByIdWithDetailsAsync(orderId)
                ?? throw new NotFoundException($"Order #{orderId} không tồn tại");

            if (order.Status != OrderStatus.PendingSaleConfirmation)
                throw new InvalidBusinessRuleException(
                    $"Chỉ sale được xác nhận khi đơn đang chờ sale (PendingSaleConfirmation). Hiện tại: {order.Status}");

            if (order.Details == null || !order.Details.Any())
                throw new InvalidBusinessRuleException("Order không có chi tiết");

            order.Status = OrderStatus.AwaitingAllocation;
            await _uow.SaveChangesAsync();

            _logger.LogInformation(
                "Order {OrderId} sale-confirmed by {UserId} → AwaitingAllocation",
                orderId, confirmedByUserId);
        }

        /// <summary>Khách chọn chờ đủ hàng (backorder) cho phần còn thiếu.</summary>
        public async Task WaitBackorderAsync(int orderId, string userId)
        {
            var order = await _orderRepo.GetByIdWithDetailsAndPaymentsAsync(orderId)
                ?? throw new NotFoundException($"Order #{orderId} không tồn tại");

            if (order.UserId != userId)
                throw new ForbiddenException("Bạn không có quyền thao tác trên đơn hàng này");

            if (order.Status != OrderStatus.PartiallyAllocated)
                throw new InvalidBusinessRuleException(
                    $"Chỉ có thể chờ backorder khi đơn đang ở trạng thái PartiallyAllocated. Hiện tại: {order.Status}");

            if (order.Details == null || !order.Details.Any(d => d.ShortageQuantity > 0))
                throw new InvalidBusinessRuleException("Đơn không còn thiếu hàng để chờ backorder");

            if (order.Payments != null && order.Payments.Any(p =>
                    p.PaymentStatus == PaymentStatus.Success ||
                    p.PaymentStatus == PaymentStatus.Refunded))
            {
                throw new InvalidBusinessRuleException("Không thể backorder: đơn đã thanh toán thành công");
            }

            // Reserved allocation cần tồn tại để biết phần nào sẽ được ship ngay.
            var reservedAllocations = await _allocationRepo.GetByOrderIdAsync(orderId, AllocationStatus.Reserved);
            if (!reservedAllocations.Any())
                throw new InvalidBusinessRuleException("Không tìm thấy allocations (Reserved) cho đơn backorder");

            order.Status = OrderStatus.BackorderWaiting;
            await _uow.SaveChangesAsync();
        }

        /// <summary>Khách chấp nhận bỏ phần còn thiếu: chỉ ship phần đã allocate được.</summary>
        public async Task CancelShortageAsync(int orderId, string userId)
        {
            await CancelShortageInternalAsync(orderId, userId, skipCustomerOwnershipCheck: false);
        }

        /// <summary>
        /// Staff allocate nốt phần còn thiếu cho backorder.
        /// Nếu đã hết thời gian chờ thì xử lý theo expiredAction.
        /// </summary>
        public async Task BackorderAllocateAsync(int orderId, string operatorUserId, BackorderExpiredAction expiredAction)
        {
            var order = await _orderRepo.GetByIdWithDetailsAndPaymentsAsync(orderId)
                ?? throw new NotFoundException($"Order #{orderId} không tồn tại");

            if (order.Status != OrderStatus.BackorderWaiting)
                throw new InvalidBusinessRuleException(
                    $"Chỉ xử lý backorder khi đơn ở trạng thái BackorderWaiting. Hiện tại: {order.Status}");

            if (order.Payments != null && order.Payments.Any(p =>
                    p.PaymentStatus == PaymentStatus.Success ||
                    p.PaymentStatus == PaymentStatus.Refunded))
            {
                throw new InvalidBusinessRuleException("Không thể allocate backorder: đơn đã thanh toán thành công");
            }

            var deadline = await GetBackorderDeadlineAtAsync(orderId);
            var isExpired = !deadline.HasValue || DateTime.UtcNow > deadline.Value;

            if (isExpired)
            {
                switch (expiredAction)
                {
                    case BackorderExpiredAction.CancelOrder:
                        await CancelOrderInternalAsync(orderId, operatorUserId, skipCustomerOwnershipCheck: true);
                        return;
                    case BackorderExpiredAction.CancelShortage:
                    default:
                        await CancelShortageInternalAsync(orderId, operatorUserId, skipCustomerOwnershipCheck: true);
                        return;
                }
            }

            // Trong thời gian chờ: allocate tiếp phần còn thiếu.
            await ConfirmOrderAsync(orderId, operatorUserId, skipCustomerOwnershipCheck: true);
        }

        /// <summary>
        /// Giữ hàng (allocate): reserve đủ box, ghi allocation + inventory transaction,
        /// cập nhật <see cref="Order.TotalAmount"/> và <see cref="Order.Status"/> → Confirmed.
        /// Chỉ gọi sau khi sale đã xác nhận (AwaitingAllocation), trừ đơn cũ còn ở AwaitingPayment.
        /// </summary>
        public async Task ConfirmOrderAsync(int orderId, string operatorUserId, bool skipCustomerOwnershipCheck = false)
        {
            _logger.LogInformation(
                "Allocate inventory for order {OrderId} by operator {UserId} (staffMode={StaffMode})",
                orderId, operatorUserId, skipCustomerOwnershipCheck);

            var order = await _orderRepo.GetByIdWithDetailsAsync(orderId)
                ?? throw new NotFoundException($"Order #{orderId} không tồn tại");

            if (!skipCustomerOwnershipCheck && order.UserId != operatorUserId)
                throw new ForbiddenException("Bạn không có quyền thao tác trên đơn hàng này");

            var canAllocate =
                order.Status == OrderStatus.AwaitingAllocation
                || order.Status == OrderStatus.AwaitingPayment
                || order.Status == OrderStatus.PartiallyAllocated
                || order.Status == OrderStatus.BackorderWaiting;

            if (!canAllocate)
                throw new InvalidBusinessRuleException(
                    $"Chỉ có thể giữ hàng khi đơn đang chờ giữ hàng / backorder. Hiện tại: {order.Status}");

            if (order.Details == null || !order.Details.Any())
                throw new InvalidBusinessRuleException("Order không có chi tiết");

            // Nếu khách đã chọn BackorderWaiting thì chặn allocate khi hết thời gian.
            if (order.Status == OrderStatus.BackorderWaiting)
            {
                var deadline = await GetBackorderDeadlineAtAsync(order.Id);
                if (!deadline.HasValue || DateTime.UtcNow > deadline.Value)
                    throw new InvalidBusinessRuleException(
                        "Backorder đã hết thời gian chờ. Vui lòng gọi endpoint xử lý timeout (cancel-shortage hoặc cancel-order) để tiếp tục.");
            }

            await _uow.BeginTransactionAsync();
            try
            {
                var originalStatus = order.Status;
                var isInitialAllocation =
                    originalStatus == OrderStatus.AwaitingAllocation
                    || originalStatus == OrderStatus.AwaitingPayment;

                var coldStorageWarnings = new List<string>();
                var (allocations, transactions, totalAmount) =
                    await ReserveBoxesForOrderAsync(order, operatorUserId, coldStorageWarnings);

                if (allocations.Count == 0)
                {
                    if (isInitialAllocation)
                    {
                        order.Status = OrderStatus.InventoryFailed;
                        await _uow.CommitAsync();

                        var shortageText = FormatShortageDetails(order);
                        _logger.LogWarning(
                            "Confirm order {OrderId} failed — insufficient stock: {Details}",
                            orderId, shortageText);

                        throw new InvalidBusinessRuleException(
                            $"Không đủ hàng tồn kho để giữ cho đơn hàng. {shortageText}");
                    }

                    // Backorder: không allocate được gì thêm trong thời điểm này.
                    await _uow.CommitAsync();
                    return;
                }

                await _allocationRepo.AddRangeAsync(allocations);
                if (transactions.Any())
                    await _inventoryTranRepo.AddRangeAsync(transactions);

                order.TotalAmount = isInitialAllocation
                    ? totalAmount
                    : order.TotalAmount + totalAmount;

                order.Status = IsOrderFullyReserved(order)
                    ? OrderStatus.Confirmed
                    : (originalStatus == OrderStatus.BackorderWaiting
                        ? OrderStatus.BackorderWaiting
                        : OrderStatus.PartiallyAllocated);

                await _uow.CommitAsync();

                _logger.LogInformation(
                    "Order {OrderId} confirmed: {BoxCount} boxes reserved, total {Total}",
                    orderId, allocations.Count, totalAmount);
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

        private async Task CancelShortageInternalAsync(int orderId, string actorUserId, bool skipCustomerOwnershipCheck)
        {
            var order = await _orderRepo.GetByIdWithDetailsAndPaymentsAsync(orderId)
                ?? throw new NotFoundException($"Order #{orderId} không tồn tại");

            if (!skipCustomerOwnershipCheck && order.UserId != actorUserId)
                throw new ForbiddenException("Bạn không có quyền thao tác trên đơn hàng này");

            if (order.Status != OrderStatus.PartiallyAllocated && order.Status != OrderStatus.BackorderWaiting)
                throw new InvalidBusinessRuleException(
                    $"Chỉ có thể cancel shortage khi đơn đang PartiallyAllocated/BackorderWaiting. Hiện tại: {order.Status}");

            var reservedAllocations = await _allocationRepo.GetByOrderIdAsync(orderId, AllocationStatus.Reserved);
            if (!reservedAllocations.Any())
                throw new InvalidBusinessRuleException("Không tìm thấy allocation (Reserved) để ship phần còn lại");

            // Chốt lại quantity theo phần đã allocate được để export & hiển thị thống nhất.
            foreach (var d in order.Details)
            {
                d.Quantity = d.FulfilledQuantity;
                d.ShortageQuantity = 0;
            }

            var unitPriceByDetailId = order.Details.ToDictionary(d => d.Id, d => d.UnitPrice);
            order.TotalAmount = reservedAllocations.Sum(a =>
                a.ReservedQuantity * unitPriceByDetailId[a.OrderDetailId]);

            order.Status = OrderStatus.Confirmed;
            await _uow.SaveChangesAsync();
        }

        private async Task CancelOrderInternalAsync(int orderId, string actorUserId, bool skipCustomerOwnershipCheck)
        {
            var order = await _orderRepo.GetByIdWithDetailsAndPaymentsAsync(orderId)
                ?? throw new NotFoundException($"Order #{orderId} không tồn tại");

            if (!skipCustomerOwnershipCheck && order.UserId != actorUserId)
                throw new ForbiddenException("Bạn không có quyền hủy đơn hàng này");

            var canCancelBeforeAllocation =
                order.Status == OrderStatus.PendingSaleConfirmation
                || order.Status == OrderStatus.AwaitingAllocation
                || order.Status == OrderStatus.PartiallyAllocated
                || order.Status == OrderStatus.AwaitingPayment
                || order.Status == OrderStatus.BackorderWaiting
                || order.Status == OrderStatus.InventoryFailed;

            if (!canCancelBeforeAllocation)
                throw new InvalidBusinessRuleException(
                    $"Chỉ có thể hủy đơn khi chưa vào bước shipping. Hiện tại: {order.Status}");

            if (order.Payments != null && order.Payments.Any(p =>
                    p.PaymentStatus == PaymentStatus.Success ||
                    p.PaymentStatus == PaymentStatus.Refunded))
            {
                throw new InvalidBusinessRuleException("Không thể hủy đơn: đơn đã thanh toán thành công");
            }

            await _uow.BeginTransactionAsync();
            try
            {
                // Nếu có payment đang Pending/Processing thì chuyển sang Cancelled.
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

        private static bool IsOrderFullyReserved(Order order) =>
            order.Details.All(d => d.ShortageQuantity == 0);

        private static string FormatShortageDetails(Order order) =>
            string.Join("; ",
                order.Details
                    .Where(d => d.ShortageQuantity > 0)
                    .Select(d => $"Variant #{d.ProductVariantId}: thiếu {d.ShortageQuantity} box"));

        private async Task ReleaseReservedBoxesAsync(IReadOnlyList<OrderAllocation> allocations)
        {
            foreach (var alloc in allocations)
            {
                var box = await _boxRepo.GetByIdAsync(alloc.BoxId);
                if (box == null) continue;
                box.Status = BoxStatus.Stored;
                await _boxRepo.UpdateAsync(box);
            }
        }

        private async Task<DateTime?> GetBackorderDeadlineAtAsync(int orderId)
        {
            // Backorder deadline lấy theo hạn hết của các allocation đã reserve (ExpiredAt).
            // Nếu có nhiều allocation, dùng mốc sớm nhất để tránh trường hợp giữ hàng hết hạn một phần.
            var allocations = await _allocationRepo.GetByOrderIdAsync(orderId, AllocationStatus.Reserved);
            var deadlines = allocations
                .Where(a => a.ExpiredAt.HasValue)
                .Select(a => a.ExpiredAt!.Value);
            return deadlines.Any() ? deadlines.Min() : null;
        }

        private async Task<(List<OrderAllocation> allocations, List<InventoryTransaction> transactions, decimal totalAmount)>
            ReserveBoxesForOrderAsync(Order order, string userId, List<string> coldStorageWarnings)
        {
            var now = DateTime.UtcNow;
            var expiredAt = now.AddHours(AllocationExpirationHours);
            var allocations = new List<OrderAllocation>();
            var transactions = new List<InventoryTransaction>();
            decimal totalAmount = 0;

            var isInitialAllocation =
                order.Status == OrderStatus.AwaitingAllocation
                || order.Status == OrderStatus.AwaitingPayment;

            foreach (var detail in order.Details)
            {
                var boxesNeeded = isInitialAllocation
                    ? (int)detail.Quantity
                    : (int)detail.ShortageQuantity;

                if (boxesNeeded <= 0)
                    continue;

                var prevFulfilled = detail.FulfilledQuantity;
                var prevShortage = detail.ShortageQuantity;
                var boxes = await _boxRepo.GetAvailableBoxesForVariantAsync(detail.ProductVariantId);
                boxes = boxes
                    .Where(b => b.IsPartial == detail.IsPartial && b.Weight == detail.BoxWeight)
                    .ToList();

                var allocated = 0;
                foreach (var box in boxes)
                {
                    if (allocated >= boxesNeeded) break;
                    if (!IsColdStorageEligible(box, coldStorageWarnings))
                        continue;

                    allocations.Add(new OrderAllocation
                    {
                        OrderId = order.Id,
                        OrderDetailId = detail.Id,
                        BoxId = box.Id,
                        ReservedQuantity = box.Weight,
                        Status = AllocationStatus.Reserved,
                        ReservedAt = now,
                        ExpiredAt = expiredAt
                    });

                    box.Status = BoxStatus.Reserved;
                    await _boxRepo.UpdateAsync(box);

                    totalAmount += box.Weight * detail.UnitPrice;
                    allocated++;
                }

                if (isInitialAllocation)
                {
                    detail.FulfilledQuantity = allocated;
                    detail.ShortageQuantity = boxesNeeded - allocated;
                }
                else
                {
                    detail.FulfilledQuantity = prevFulfilled + allocated;
                    detail.ShortageQuantity = prevShortage - allocated;
                }
            }

            return (allocations, transactions, totalAmount);
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
