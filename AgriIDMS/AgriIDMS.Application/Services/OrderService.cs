using AgriIDMS.Application.DTOs.Order;
using AgriIDMS.Application.Exceptions;
using AgriIDMS.Application.Interfaces;
using AgriIDMS.Domain.Entities;
using AgriIDMS.Domain.Enums;
using AgriIDMS.Domain.Exceptions;
using AgriIDMS.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

namespace AgriIDMS.Application.Services
{
    public class OrderService : IOrderService
    {
        private readonly ICartRepository _cartRepo;
        private readonly IOrderRepository _orderRepo;
        private readonly IBoxRepository _boxRepo;
        private readonly IProductVariantRepository _variantRepo;
        private readonly IOrderAllocationRepository _allocationRepo;
        private readonly IInventoryTransactionRepository _inventoryTranRepo;
        private readonly INotificationService _notificationService;
        private readonly IUserRepository _userRepo;
        private readonly IUnitOfWork _uow;
        private readonly ILogger<OrderService> _logger;
        private readonly int _nearExpiryDiscountDays;
        private readonly decimal _nearExpiryDiscountPercent;
        private const decimal PriceComparisonTolerance = 0.0001m;
        private const int CompleteAfterDeliveredDays = 4;

        private const int AllocationExpirationHours = 24;

        public OrderService(
            ICartRepository cartRepo,
            IOrderRepository orderRepo,
            IBoxRepository boxRepo,
            IProductVariantRepository variantRepo,
            IOrderAllocationRepository allocationRepo,
            IInventoryTransactionRepository inventoryTranRepo,
            INotificationService notificationService,
            IUserRepository userRepo,
            IUnitOfWork uow,
            IConfiguration config,
            ILogger<OrderService> logger)
        {
            _cartRepo = cartRepo;
            _orderRepo = orderRepo;
            _boxRepo = boxRepo;
            _variantRepo = variantRepo;
            _allocationRepo = allocationRepo;
            _inventoryTranRepo = inventoryTranRepo;
            _notificationService = notificationService;
            _userRepo = userRepo;
            _uow = uow;
            _nearExpiryDiscountDays = int.TryParse(config["Pricing:NearExpiryDiscountDays"], out var days)
                ? days
                : 0;
            _nearExpiryDiscountPercent = decimal.TryParse(
                config["Pricing:NearExpiryDiscountPercent"],
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out var percent)
                ? percent
                : 0m;
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
                Source = o.Source.ToString(),
                CreatedAt = o.CreatedAt,
                ItemCount = o.Details?.Count ?? 0,
                LatestPaymentStatus = o.Payments?
                    .OrderByDescending(p => p.CreatedAt)
                    .FirstOrDefault()?
                    .PaymentStatus
                    .ToString()
            }).ToList();
        }

        public async Task<IList<OrderListItemDto>> GetPendingSaleConfirmOrdersAsync(GetPendingSaleConfirmOrdersQuery query)
        {
            query ??= new GetPendingSaleConfirmOrdersQuery();
            var take = Math.Clamp(query.Take, 1, 200);
            var skip = Math.Max(0, query.Skip);

            var orders = await _orderRepo.GetPendingSaleConfirmationOrdersAsync(
                query.CustomerUserId,
                skip,
                take);

            return orders.Select(o => new OrderListItemDto
            {
                OrderId = o.Id,
                TotalAmount = o.TotalAmount,
                Status = o.Status.ToString(),
                Source = o.Source.ToString(),
                CreatedAt = o.CreatedAt,
                ItemCount = o.Details?.Count ?? 0,
                LatestPaymentStatus = o.Payments?
                    .OrderByDescending(p => p.CreatedAt)
                    .FirstOrDefault()?
                    .PaymentStatus
                    .ToString()
            }).ToList();
        }

        public async Task<IList<PaidPendingExportOrderListItemDto>> GetPaidPendingExportOrdersAsync(GetPaidPendingExportOrdersQuery query)
        {
            query ??= new GetPaidPendingExportOrdersQuery();
            var take = Math.Clamp(query.Take, 1, 200);
            var skip = Math.Max(0, query.Skip);

            OrderSource? source = null;
            if (!string.IsNullOrWhiteSpace(query.Source))
            {
                if (!Enum.TryParse<OrderSource>(query.Source, true, out var parsedSource))
                    throw new InvalidBusinessRuleException("Source không hợp lệ. Chỉ nhận Online hoặc POS.");
                source = parsedSource;
            }

            var orders = await _orderRepo.GetPaidPendingExportOrdersAsync(
                query.OrderId,
                source,
                skip,
                take,
                query.Sort);

            return orders.Select(o =>
            {
                var activeExport = o.ExportReceipts?
                    .Where(e => e.Status != ExportStatus.Cancelled)
                    .OrderByDescending(e => e.Id)
                    .FirstOrDefault();

                DateTime? paidAt = null;
                if (o.Payments != null && o.Payments.Count > 0)
                {
                    var paidDates = o.Payments.Where(p => p.PaidAt.HasValue).Select(p => p.PaidAt!.Value).ToList();
                    if (paidDates.Count > 0)
                        paidAt = paidDates.Max();
                }

                return new PaidPendingExportOrderListItemDto
                {
                    OrderId = o.Id,
                    Status = o.Status.ToString(),
                    TotalAmount = o.TotalAmount,
                    PaidAt = paidAt,
                    CreatedAt = o.CreatedAt,
                    ItemCount = o.Details?.Count ?? 0,
                    Source = o.Source.ToString(),
                    HasExportReceipt = activeExport != null,
                    ExportReceiptId = activeExport?.Id,
                    ExportStatus = activeExport?.Status.ToString(),
                    ExportCode = activeExport?.ExportCode
                };
            }).ToList();
        }

        public async Task<IList<OrderListItemDto>> GetPendingAllocationOrdersAsync(GetPendingAllocationOrdersQuery query)
        {
            query ??= new GetPendingAllocationOrdersQuery();
            var take = Math.Clamp(query.Take, 1, 200);
            var skip = Math.Max(0, query.Skip);

            OrderSource? source = null;
            if (!string.IsNullOrWhiteSpace(query.Source))
            {
                if (!Enum.TryParse<OrderSource>(query.Source, true, out var parsedSource))
                    throw new InvalidBusinessRuleException("Source không hợp lệ. Chỉ nhận Online hoặc POS.");
                source = parsedSource;
            }

            var orders = await _orderRepo.GetPendingAllocationOrdersAsync(
                query.CustomerUserId,
                source,
                skip,
                take);

            return orders.Select(o => new OrderListItemDto
            {
                OrderId = o.Id,
                TotalAmount = o.TotalAmount,
                Status = o.Status.ToString(),
                Source = o.Source.ToString(),
                CreatedAt = o.CreatedAt,
                ItemCount = o.Details?.Count ?? 0,
                LatestPaymentStatus = o.Payments?
                    .OrderByDescending(p => p.CreatedAt)
                    .FirstOrDefault()?
                    .PaymentStatus
                    .ToString()
            }).ToList();
        }

        public async Task<IList<OrderListItemDto>> GetPendingWarehouseConfirmOrdersAsync(GetPendingAllocationOrdersQuery query)
        {
            query ??= new GetPendingAllocationOrdersQuery();
            var take = Math.Clamp(query.Take, 1, 200);
            var skip = Math.Max(0, query.Skip);

            OrderSource? source = null;
            if (!string.IsNullOrWhiteSpace(query.Source))
            {
                if (!Enum.TryParse<OrderSource>(query.Source, true, out var parsedSource))
                    throw new InvalidBusinessRuleException("Source không hợp lệ. Chỉ nhận Online hoặc POS.");
                source = parsedSource;
            }

            var orders = await _orderRepo.GetPendingWarehouseConfirmOrdersAsync(
                query.CustomerUserId,
                source,
                skip,
                take);

            return orders.Select(o => new OrderListItemDto
            {
                OrderId = o.Id,
                TotalAmount = o.TotalAmount,
                Status = o.Status.ToString(),
                Source = o.Source.ToString(),
                CreatedAt = o.CreatedAt,
                ItemCount = o.Details?.Count ?? 0,
                LatestPaymentStatus = o.Payments?
                    .OrderByDescending(p => p.CreatedAt)
                    .FirstOrDefault()?
                    .PaymentStatus
                    .ToString()
            }).ToList();
        }

        public async Task<IList<OrderListItemDto>> GetPendingCustomerDecisionOrdersAsync(GetPendingAllocationOrdersQuery query)
        {
            query ??= new GetPendingAllocationOrdersQuery();
            var take = Math.Clamp(query.Take, 1, 200);
            var skip = Math.Max(0, query.Skip);

            OrderSource? source = null;
            if (!string.IsNullOrWhiteSpace(query.Source))
            {
                if (!Enum.TryParse<OrderSource>(query.Source, true, out var parsedSource))
                    throw new InvalidBusinessRuleException("Source không hợp lệ. Chỉ nhận Online hoặc POS.");
                source = parsedSource;
            }

            var orders = await _orderRepo.GetPendingCustomerDecisionOrdersAsync(
                query.CustomerUserId,
                source,
                skip,
                take);

            return orders.Select(o => new OrderListItemDto
            {
                OrderId = o.Id,
                TotalAmount = o.TotalAmount,
                Status = o.Status.ToString(),
                Source = o.Source.ToString(),
                CreatedAt = o.CreatedAt,
                ItemCount = o.Details?.Count ?? 0,
                LatestPaymentStatus = o.Payments?
                    .OrderByDescending(p => p.CreatedAt)
                    .FirstOrDefault()?
                    .PaymentStatus
                    .ToString()
            }).ToList();
        }

        public async Task<IList<BackorderWaitingListItemDto>> GetBackorderWaitingOrdersAsync(GetPendingAllocationOrdersQuery query)
        {
            query ??= new GetPendingAllocationOrdersQuery();
            var take = Math.Clamp(query.Take, 1, 200);
            var skip = Math.Max(0, query.Skip);

            OrderSource? source = null;
            if (!string.IsNullOrWhiteSpace(query.Source))
            {
                if (!Enum.TryParse<OrderSource>(query.Source, true, out var parsedSource))
                    throw new InvalidBusinessRuleException("Source không hợp lệ. Chỉ nhận Online hoặc POS.");
                source = parsedSource;
            }

            var orders = await _orderRepo.GetBackorderWaitingOrdersAsync(
                query.CustomerUserId,
                source,
                skip,
                take);

            var now = DateTime.UtcNow;
            var result = new List<BackorderWaitingListItemDto>();
            foreach (var o in orders)
            {
                var deadline = await GetBackorderDeadlineAtAsync(o.Id);
                var totalRequested = o.Details?.Sum(d => d.Quantity) ?? 0m;
                var totalFulfilled = o.Details?.Sum(d => d.FulfilledQuantity) ?? 0m;
                var totalShortage = o.Details?.Sum(d => d.ShortageQuantity) ?? 0m;
                var isOverdue = !deadline.HasValue || now > deadline.Value;

                result.Add(new BackorderWaitingListItemDto
                {
                    OrderId = o.Id,
                    CustomerUserId = o.UserId,
                    CustomerName = null,
                    Source = o.Source.ToString(),
                    Status = o.Status.ToString(),
                    CreatedAt = o.CreatedAt,
                    BackorderDeadlineAt = deadline,
                    IsOverdue = isOverdue,
                    TotalRequestedBoxes = totalRequested,
                    TotalFulfilledBoxes = totalFulfilled,
                    TotalShortageBoxes = totalShortage,
                    TotalAmount = o.TotalAmount,
                    ItemCount = o.Details?.Count ?? 0,
                    LatestPaymentStatus = o.Payments?
                        .OrderByDescending(p => p.CreatedAt)
                        .FirstOrDefault()?
                        .PaymentStatus
                        .ToString(),
                    NextRecommendedAction = isOverdue
                        ? "choose_expired_action"
                        : "allocate_backorder"
                });
            }

            return result;
        }

        public async Task<BackorderWaitingDetailDto> GetBackorderWaitingOrderDetailAsync(int orderId)
        {
            var order = await _orderRepo.GetByIdWithDetailsAndPaymentsAsync(orderId)
                ?? throw new NotFoundException($"Order #{orderId} không tồn tại");

            if (order.Status != OrderStatus.BackorderWaiting)
                throw new InvalidBusinessRuleException(
                    $"Chỉ xem chi tiết backorder-waiting khi đơn ở trạng thái BackorderWaiting. Hiện tại: {order.Status}");

            var deadline = await GetBackorderDeadlineAtAsync(order.Id);
            var now = DateTime.UtcNow;
            var isOverdue = !deadline.HasValue || now > deadline.Value;

            var reservedAllocations = await _allocationRepo.GetByOrderIdWithDetailsAsync(orderId, AllocationStatus.Reserved);
            var latestPaymentStatus = order.Payments?
                .OrderByDescending(p => p.CreatedAt)
                .FirstOrDefault()?
                .PaymentStatus
                .ToString();

            var items = order.Details.Select(d => new BackorderWaitingDetailItemDto
            {
                OrderDetailId = d.Id,
                ProductVariantId = d.ProductVariantId,
                ProductName = d.ProductVariant?.Product?.Name ?? string.Empty,
                Grade = d.ProductVariant?.Grade.ToString() ?? string.Empty,
                BoxWeight = d.BoxWeight,
                IsPartial = d.IsPartial,
                RequestedQuantity = d.Quantity,
                FulfilledQuantity = d.FulfilledQuantity,
                ShortageQuantity = d.ShortageQuantity,
                UnitPrice = d.UnitPrice
            }).ToList();

            var totalRequested = items.Sum(i => i.RequestedQuantity);
            var totalFulfilled = items.Sum(i => i.FulfilledQuantity);
            var totalShortage = items.Sum(i => i.ShortageQuantity);

            return new BackorderWaitingDetailDto
            {
                OrderId = order.Id,
                CustomerUserId = order.UserId,
                CustomerName = null,
                Source = order.Source.ToString(),
                Status = order.Status.ToString(),
                CreatedAt = order.CreatedAt,
                BackorderDeadlineAt = deadline,
                IsOverdue = isOverdue,
                TotalAmount = order.TotalAmount,
                LatestPaymentStatus = latestPaymentStatus,
                Summary = new BackorderWaitingSummaryDto
                {
                    TotalRequestedBoxes = totalRequested,
                    TotalFulfilledBoxes = totalFulfilled,
                    TotalShortageBoxes = totalShortage,
                    IsFullyAllocated = totalShortage <= 0
                },
                Items = items,
                ReservedAllocations = reservedAllocations.Select(a => new BackorderWaitingReservedAllocationDto
                {
                    AllocationId = a.Id,
                    OrderDetailId = a.OrderDetailId,
                    BoxId = a.BoxId,
                    BoxCode = a.Box?.BoxCode ?? string.Empty,
                    ReservedQuantity = a.ReservedQuantity,
                    Status = a.Status.ToString(),
                    ReservedAt = a.ReservedAt,
                    ExpiredAt = a.ExpiredAt
                }).ToList(),
                AllowedActions = isOverdue
                    ? new List<string> { "cancel_shortage", "cancel_order" }
                    : new List<string> { "allocate_backorder" }
            };
        }

        public async Task<IList<OrderListItemDto>> GetConfirmedAllocationOrdersAsync(GetPendingAllocationOrdersQuery query)
        {
            query ??= new GetPendingAllocationOrdersQuery();
            var take = Math.Clamp(query.Take, 1, 200);
            var skip = Math.Max(0, query.Skip);

            OrderSource? source = null;
            if (!string.IsNullOrWhiteSpace(query.Source))
            {
                if (!Enum.TryParse<OrderSource>(query.Source, true, out var parsedSource))
                    throw new InvalidBusinessRuleException("Source không hợp lệ. Chỉ nhận Online hoặc POS.");
                source = parsedSource;
            }

            var orders = await _orderRepo.GetConfirmedAllocationOrdersAsync(
                query.CustomerUserId,
                source,
                skip,
                take);

            return orders.Select(o => new OrderListItemDto
            {
                OrderId = o.Id,
                TotalAmount = o.TotalAmount,
                Status = o.Status.ToString(),
                Source = o.Source.ToString(),
                CreatedAt = o.CreatedAt,
                ItemCount = o.Details?.Count ?? 0,
                LatestPaymentStatus = o.Payments?
                    .OrderByDescending(p => p.CreatedAt)
                    .FirstOrDefault()?
                    .PaymentStatus
                    .ToString()
            }).ToList();
        }

        public async Task<AllocationProposalOverviewDto> GetAllocationProposalsAsync(int orderId)
        {
            var order = await _orderRepo.GetByIdWithDetailsAsync(orderId)
                ?? throw new NotFoundException($"Order #{orderId} không tồn tại");

            if (order.Status != OrderStatus.PendingWarehouseConfirm
                && order.Status != OrderStatus.PartiallyAllocated
                && order.Status != OrderStatus.BackorderWaiting
                && order.Status != OrderStatus.Confirmed)
            {
                throw new InvalidBusinessRuleException(
                    $"Chỉ xem proposal khi đơn đang chờ kho xác nhận/backorder. Hiện tại: {order.Status}");
            }

            // For fully allocated orders (Confirmed), FE should still see the reserved boxes.
            // Reuse this endpoint by switching from Proposed -> Reserved.
            var allocationStatus = order.Status == OrderStatus.Confirmed
                ? AllocationStatus.Reserved
                : AllocationStatus.Proposed;

            var proposals = await _allocationRepo.GetByOrderIdWithDetailsAsync(orderId, allocationStatus);
            var proposalItems = proposals.Select(p => new AllocationProposalItemDto
            {
                AllocationId = p.Id,
                OrderDetailId = p.OrderDetailId,
                ProductVariantId = p.OrderDetail.ProductVariantId,
                ProductName = p.OrderDetail.ProductVariant?.Product?.Name ?? string.Empty,
                Grade = p.OrderDetail.ProductVariant?.Grade.ToString() ?? string.Empty,
                BoxId = p.BoxId,
                BoxCode = p.Box?.BoxCode ?? string.Empty,
                BoxWeight = p.Box?.Weight ?? 0,
                IsPartial = p.Box?.IsPartial ?? false,
                ExpiryDate = p.Box?.Lot?.ExpiryDate,
                Status = p.Status.ToString()
            }).ToList();

            var proposedByDetailId = proposals
                .GroupBy(p => p.OrderDetailId)
                .ToDictionary(g => g.Key, g => g.Count());

            var detailSummaries = order.Details.Select(d =>
            {
                proposedByDetailId.TryGetValue(d.Id, out var proposedQty);

                var requestedQty = order.Status == OrderStatus.PartiallyAllocated || order.Status == OrderStatus.BackorderWaiting
                    ? (int)d.ShortageQuantity
                    : (int)d.Quantity;

                var shortage = Math.Max(0, requestedQty - proposedQty);
                return new AllocationProposalDetailSummaryDto
                {
                    OrderDetailId = d.Id,
                    ProductVariantId = d.ProductVariantId,
                    ProductName = d.ProductVariant?.Product?.Name ?? string.Empty,
                    Grade = d.ProductVariant?.Grade.ToString() ?? string.Empty,
                    BoxWeight = d.BoxWeight,
                    IsPartial = d.IsPartial,
                    RequestedQuantity = requestedQty,
                    ProposedQuantity = proposedQty,
                    ShortageQuantity = shortage
                };
            }).ToList();

            return new AllocationProposalOverviewDto
            {
                OrderId = order.Id,
                OrderStatus = order.Status.ToString(),
                TotalRequestedBoxes = detailSummaries.Sum(x => x.RequestedQuantity),
                TotalProposedBoxes = detailSummaries.Sum(x => x.ProposedQuantity),
                TotalShortageBoxes = detailSummaries.Sum(x => x.ShortageQuantity),
                Details = detailSummaries,
                Proposals = proposalItems
            };
        }

        public async Task<AllocationHistoryDto> GetAllocationHistoryAsync(int orderId)
        {
            var order = await _orderRepo.GetByIdWithDetailsAsync(orderId)
                ?? throw new NotFoundException($"Order #{orderId} không tồn tại");

            var allocations = await _allocationRepo.GetByOrderIdWithDetailsAsync(orderId);
            var items = allocations.Select(a => new AllocationHistoryItemDto
            {
                AllocationId = a.Id,
                OrderDetailId = a.OrderDetailId,
                ProductVariantId = a.OrderDetail?.ProductVariantId ?? 0,
                ProductName = a.OrderDetail?.ProductVariant?.Product?.Name ?? string.Empty,
                Grade = a.OrderDetail?.ProductVariant?.Grade.ToString() ?? string.Empty,
                BoxId = a.BoxId,
                BoxCode = a.Box?.BoxCode ?? string.Empty,
                BoxWeight = a.Box?.Weight ?? 0,
                IsPartial = a.Box?.IsPartial ?? false,
                ReservedAt = a.ReservedAt,
                ExpiredAt = a.ExpiredAt,
                Status = a.Status.ToString()
            }).ToList();

            return new AllocationHistoryDto
            {
                OrderId = order.Id,
                OrderStatus = order.Status.ToString(),
                Items = items
            };
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
                Source = order.Source.ToString(),
                CreatedAt = order.CreatedAt,
                LatestPaymentStatus = order.Payments?
                    .OrderByDescending(p => p.CreatedAt)
                    .FirstOrDefault()?
                    .PaymentStatus
                    .ToString(),
                Recipient = ToRecipientSnapshot(order),
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

        public async Task ConfirmDeliveredAsync(int orderId, string operatorUserId)
        {
            var order = await _orderRepo.GetByIdWithPaymentsAsync(orderId)
                ?? throw new NotFoundException($"Order #{orderId} không tồn tại");

            if (!IsDelivery(order))
                throw new InvalidBusinessRuleException("Chỉ đơn Delivery mới được xác nhận delivered");

            // Idempotent: gọi nhiều lần không làm sai dữ liệu
            if (order.Status == OrderStatus.Delivered)
                return;

            if (order.Status != OrderStatus.Shipping)
                throw new InvalidBusinessRuleException(
                    $"Chỉ xác nhận giao thành công khi đơn ở trạng thái Shipping. Hiện tại: {order.Status}");

            var latestPayment = order.Payments?
                .OrderByDescending(p => p.CreatedAt)
                .FirstOrDefault();

            if (latestPayment == null)
                throw new InvalidBusinessRuleException("Không tìm thấy thông tin thanh toán của đơn hàng");

            if (latestPayment.PaymentMethod == PaymentMethod.COD)
            {
                if (latestPayment.PaymentStatus == PaymentStatus.Pending)
                {
                    latestPayment.PaymentStatus = PaymentStatus.Paid;
                    latestPayment.PaidAt = DateTime.UtcNow;
                }
                else if (latestPayment.PaymentStatus != PaymentStatus.Paid)
                {
                    throw new InvalidBusinessRuleException(
                        $"Không thể xác nhận Delivered khi COD có trạng thái thanh toán {latestPayment.PaymentStatus}");
                }
            }
            else if (latestPayment.PaymentStatus != PaymentStatus.Paid)
            {
                throw new InvalidBusinessRuleException("Đơn thanh toán online chưa Paid, không thể xác nhận Delivered");
            }

            order.Status = OrderStatus.Delivered;
            order.DeliveredAt = DateTime.UtcNow;
            await _uow.SaveChangesAsync();
        }

        public async Task ConfirmFailedDeliveryAsync(int orderId, string operatorUserId)
        {
            var order = await _orderRepo.GetByIdAsync(orderId)
                ?? throw new NotFoundException($"Order #{orderId} không tồn tại");

            if (!IsDelivery(order))
                throw new InvalidBusinessRuleException("Chỉ đơn Delivery mới được cập nhật failed delivery");

            if (order.Status == OrderStatus.FailedDelivery)
                return;

            if (order.Status != OrderStatus.Shipping)
                throw new InvalidBusinessRuleException(
                    $"Chỉ đánh dấu giao thất bại khi đơn ở trạng thái Shipping. Hiện tại: {order.Status}");

            order.Status = OrderStatus.FailedDelivery;
            await _uow.SaveChangesAsync();
        }

        public async Task ConfirmReturnedAsync(int orderId, string operatorUserId)
        {
            var order = await _orderRepo.GetByIdAsync(orderId)
                ?? throw new NotFoundException($"Order #{orderId} không tồn tại");

            if (!IsDelivery(order))
                throw new InvalidBusinessRuleException("Chỉ đơn Delivery mới được cập nhật returned");

            if (order.Status == OrderStatus.Returned)
                return;

            if (order.Status != OrderStatus.FailedDelivery && order.Status != OrderStatus.Shipping)
                throw new InvalidBusinessRuleException(
                    $"Chỉ đánh dấu Returned khi đơn ở trạng thái Shipping/FailedDelivery. Hiện tại: {order.Status}");

            order.Status = OrderStatus.Returned;
            await _uow.SaveChangesAsync();
        }

        public async Task ConfirmCODPaidAsync(int orderId, string operatorUserId)
        {
            var order = await _orderRepo.GetByIdWithPaymentsAsync(orderId)
                ?? throw new NotFoundException($"Order #{orderId} không tồn tại");

            var latestCodPayment = order.Payments?
                .Where(p => p.PaymentMethod == PaymentMethod.COD)
                .OrderByDescending(p => p.CreatedAt)
                .FirstOrDefault();

            if (latestCodPayment == null)
                throw new InvalidBusinessRuleException("Đơn không có thanh toán COD");

            if (latestCodPayment.PaymentStatus == PaymentStatus.Paid)
                return;

            if (latestCodPayment.PaymentStatus != PaymentStatus.Pending)
                throw new InvalidBusinessRuleException(
                    $"Không thể xác nhận COD Paid từ trạng thái {latestCodPayment.PaymentStatus}");

            latestCodPayment.PaymentStatus = PaymentStatus.Paid;
            latestCodPayment.PaidAt = DateTime.UtcNow;

            if (IsTakeAway(order) && order.Status != OrderStatus.Delivered)
            {
                order.Status = OrderStatus.Delivered;
                order.DeliveredAt = DateTime.UtcNow;
            }

            await _uow.SaveChangesAsync();
        }

        public async Task<bool> CheckCanCompleteAsync(int orderId)
        {
            var order = await _orderRepo.GetByIdAsync(orderId)
                ?? throw new NotFoundException($"Order #{orderId} không tồn tại");

            var hasPendingComplaint = await _orderRepo.HasPendingComplaintAsync(orderId);
            return CheckCanComplete(order, hasPendingComplaint, DateTime.UtcNow);
        }

        public async Task<int> AutoCompleteOrdersAsync()
        {
            var thresholdUtc = DateTime.UtcNow.AddDays(-CompleteAfterDeliveredDays);
            var candidates = await _orderRepo.GetDeliveredOrdersEligibleForCompletionAsync(thresholdUtc);

            if (candidates.Count == 0)
                return 0;

            var completedCount = 0;
            foreach (var order in candidates)
            {
                // Idempotent safety check in case data changed between query and update
                if (order.Status != OrderStatus.Delivered)
                    continue;

                order.Status = OrderStatus.Completed;
                completedCount++;
            }

            if (completedCount > 0)
                await _uow.SaveChangesAsync();

            return completedCount;
        }

        public async Task<IList<OverdueBackorderItemDto>> GetOverdueBackordersAsync()
        {
            var now = DateTime.UtcNow;
            var orders = await _orderRepo.GetOverdueBackordersAsync(now);

            return orders.Select(o =>
            {
                var reservedAllocations = o.Allocations
                    .Where(a => a.Status == AllocationStatus.Reserved)
                    .ToList();

                var deadline = reservedAllocations
                    .Where(a => a.ExpiredAt.HasValue)
                    .Select(a => a.ExpiredAt!.Value)
                    .DefaultIfEmpty(now)
                    .Min();

                return new OverdueBackorderItemDto
                {
                    OrderId = o.Id,
                    CustomerUserId = o.UserId,
                    CreatedAt = o.CreatedAt,
                    BackorderDeadlineAt = deadline,
                    TotalShortageQuantity = o.Details.Sum(d => d.ShortageQuantity),
                    TotalReservedQuantity = reservedAllocations.Sum(a => a.ReservedQuantity),
                    CurrentTotalAmount = o.TotalAmount,
                    Status = o.Status.ToString()
                };
            }).ToList();
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
                || order.Status == OrderStatus.PendingWarehouseConfirm
                || order.Status == OrderStatus.PartiallyAllocated
                || order.Status == OrderStatus.AwaitingPayment
                || order.Status == OrderStatus.BackorderWaiting
                || order.Status == OrderStatus.InventoryFailed;

            if (!canCancelBeforeAllocation)
                throw new InvalidBusinessRuleException(
                    $"Chỉ có thể hủy đơn khi chưa giữ hàng / chưa thanh toán thành công. Hiện tại: {order.Status}");

            if (order.Payments != null && order.Payments.Any(p =>
                    p.PaymentStatus == PaymentStatus.Paid ||
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

        public async Task<OrderCheckoutDefaultsDto> GetOrderCheckoutDefaultsAsync(string userId)
        {
            var user = await _userRepo.GetByIdAsync(userId)
                ?? throw new NotFoundException("Không tìm thấy người dùng");

            return new OrderCheckoutDefaultsDto
            {
                FullName = user.FullName?.Trim() ?? string.Empty,
                Phone = user.PhoneNumber?.Trim() ?? string.Empty,
                Address = user.Address?.Trim() ?? string.Empty
            };
        }

        public async Task<CreateOrderFromCartResponse> CreateOrderFromCartAsync(string userId, OrderRecipientCheckoutDto recipient)
        {
            _logger.LogInformation("Creating order from cart for user {UserId}", userId);

            if (recipient == null)
                throw new InvalidBusinessRuleException("Thiếu thông tin người nhận (checkout).");

            var cart = await _cartRepo.GetByUserIdWithItemsAsync(userId);
            if (cart == null || cart.Items == null || !cart.Items.Any())
                throw new InvalidBusinessRuleException("Giỏ hàng trống");

            var now = DateTime.UtcNow;

            await _uow.BeginTransactionAsync();
            try
            {
                decimal estimatedTotal = 0;
                var nearExpiryEligibilityCache = new Dictionary<int, (bool IsNearExpiry, decimal EffectivePercent)>();
                var unitPriceByType = new Dictionary<(int ProductVariantId, decimal BoxWeight, bool IsPartial), decimal>();
                var order = new Order
                {
                    UserId = userId,
                    CreatedAt = now,
                    Source = OrderSource.Online,
                    FulfillmentType = FulfillmentType.Delivery,
                    Status = OrderStatus.PendingSaleConfirmation
                };
                ApplyRecipientToOrder(order, recipient);

                foreach (var item in cart.Items)
                {
                    var cartUnitPricePerKg = NormalizeCartUnitPricePerKg(item.UnitPrice, item.BoxWeight, item.ProductVariant?.Price);
                    var unitPrice = await ApplyNearExpiryDiscountIfEligibleAsync(
                        item.ProductVariantId,
                        cartUnitPricePerKg,
                        nearExpiryEligibilityCache);

                    var detail = new OrderDetail
                    {
                        ProductVariantId = item.ProductVariantId,
                        BoxWeight = item.BoxWeight,
                        IsPartial = item.IsPartial,
                        Quantity = (int)item.Quantity,
                        UnitPrice = unitPrice,
                        FulfilledQuantity = 0,
                        ShortageQuantity = 0
                    };
                    order.Details.Add(detail);
                    estimatedTotal += detail.Quantity * detail.BoxWeight * detail.UnitPrice;
                    unitPriceByType[(item.ProductVariantId, item.BoxWeight, item.IsPartial)] = detail.UnitPrice;
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
                    UnitPrice = unitPriceByType[(i.ProductVariantId, i.BoxWeight, i.IsPartial)]
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
                    Recipient = ToRecipientSnapshot(order),
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

        public async Task<CreateOrderFromCartResponse> CreateOrderFromCartByVariantIdsAsync(
            string userId,
            IList<CreateOrderFromCartByVariantIdsRequest> requestItems,
            OrderRecipientCheckoutDto recipient)
        {
            _logger.LogInformation(
                "Creating order from cart variants ({Count}) for user {UserId}",
                requestItems?.Count ?? 0, userId);

            if (requestItems == null || !requestItems.Any())
                throw new InvalidBusinessRuleException("Bạn phải chọn ít nhất 1 loại sản phẩm");

            if (recipient == null)
                throw new InvalidBusinessRuleException("Thiếu thông tin người nhận (checkout).");

            if (requestItems.Any(x => x == null))
                throw new InvalidBusinessRuleException("Danh sách sản phẩm không hợp lệ (có dòng trống).");

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
                var nearExpiryEligibilityCache = new Dictionary<int, (bool IsNearExpiry, decimal EffectivePercent)>();

                var order = new Order
                {
                    UserId = userId,
                    CreatedAt = now,
                    Source = OrderSource.Online,
                    FulfillmentType = FulfillmentType.Delivery,
                    Status = OrderStatus.PendingSaleConfirmation
                };
                ApplyRecipientToOrder(order, recipient);

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
                            UnitPrice = await ApplyNearExpiryDiscountIfEligibleAsync(
                                item.ProductVariantId,
                                NormalizeCartUnitPricePerKg(item.UnitPrice, item.BoxWeight, item.ProductVariant?.Price),
                                nearExpiryEligibilityCache),
                            FulfilledQuantity = 0,
                            ShortageQuantity = 0
                        };

                        order.Details.Add(detail);
                        estimatedTotal += detail.Quantity * detail.BoxWeight * detail.UnitPrice;

                        orderItems.Add(new OrderItemDto
                        {
                            ProductVariantId = item.ProductVariantId,
                            ProductName = item.ProductVariant?.Product?.Name ?? string.Empty,
                            Grade = item.ProductVariant?.Grade.ToString() ?? string.Empty,
                            BoxWeight = item.BoxWeight,
                            IsPartial = item.IsPartial,
                            Quantity = qtyToTake,
                            UnitPrice = detail.UnitPrice
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
                    Recipient = ToRecipientSnapshot(order),
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

        public async Task<CreateOrderFromCartResponse> CreatePosOrderAsync(string operatorUserId, CreatePosOrderRequest request)
        {
            _logger.LogInformation("Creating POS order by operator {UserId}", operatorUserId);

            if (request == null || request.Items == null || !request.Items.Any())
                throw new InvalidBusinessRuleException("Đơn POS phải có ít nhất 1 dòng sản phẩm");

            var posCustomer = await ResolvePosCustomerAsync(request, operatorUserId);

            var now = DateTime.UtcNow;
            var nearExpiryEligibilityCache = new Dictionary<int, (bool IsNearExpiry, decimal EffectivePercent)>();
            await _uow.BeginTransactionAsync();
            try
            {
                decimal total = 0m;
                var order = new Order
                {
                    // POS order được tạo bởi staff; thông tin customer được lưu ở bộ field riêng.
                    UserId = operatorUserId,
                    CreatedAt = now,
                    Source = OrderSource.POS,
                    FulfillmentType = request.FulfillmentType,
                    Status = request.FulfillmentType == FulfillmentType.TakeAway
                        ? OrderStatus.Confirmed
                        : OrderStatus.AwaitingAllocation,
                    CustomerUserId = posCustomer.CustomerUserId,
                    CustomerName = posCustomer.CustomerName,
                    CustomerPhone = posCustomer.CustomerPhone,
                    IsGuest = posCustomer.IsGuest,
                    RecipientFullName = string.Empty,
                    RecipientPhone = string.Empty,
                    RecipientAddress = string.Empty
                };

                var responseItems = new List<OrderItemDto>();
                foreach (var item in request.Items)
                {
                    if (item.Quantity <= 0)
                        throw new InvalidBusinessRuleException("Số lượng phải lớn hơn 0");
                    if (item.BoxWeight <= 0)
                        throw new InvalidBusinessRuleException("BoxWeight phải lớn hơn 0");

                    var availableBoxes = await _boxRepo.GetAvailableBoxCountByVariantAndTypeAsync(
                        item.ProductVariantId,
                        item.IsPartial,
                        item.BoxWeight,
                        includeOfflineOnly: true);
                    if (item.Quantity > availableBoxes)
                        throw new InvalidBusinessRuleException(
                            $"Số lượng ({item.Quantity} thùng) vượt quá tồn khả dụng ({availableBoxes} thùng) cho biến thể và loại thùng đã chọn.");

                    var variant = await _variantRepo.GetProductVariantByIdAsync(item.ProductVariantId)
                        ?? throw new NotFoundException($"ProductVariant #{item.ProductVariantId} không tồn tại");

                    var baseUnitPrice = item.UnitPrice ?? variant.Price;
                    var unitPrice = item.UnitPrice.HasValue
                        ? baseUnitPrice
                        : await ApplyNearExpiryDiscountIfEligibleAsync(
                            item.ProductVariantId,
                            baseUnitPrice,
                            nearExpiryEligibilityCache,
                            includeOfflineOnly: true);
                    if (unitPrice <= 0)
                        throw new InvalidBusinessRuleException("Đơn giá phải lớn hơn 0");

                    var detail = new OrderDetail
                    {
                        ProductVariantId = item.ProductVariantId,
                        BoxWeight = item.BoxWeight,
                        IsPartial = item.IsPartial,
                        Quantity = item.Quantity,
                        UnitPrice = unitPrice,
                        FulfilledQuantity = 0,
                        ShortageQuantity = 0
                    };

                    order.Details.Add(detail);
                    total += detail.Quantity * detail.BoxWeight * detail.UnitPrice;

                    responseItems.Add(new OrderItemDto
                    {
                        ProductVariantId = detail.ProductVariantId,
                        ProductName = variant.Product?.Name ?? string.Empty,
                        Grade = variant.Grade.ToString(),
                        BoxWeight = detail.BoxWeight,
                        IsPartial = detail.IsPartial,
                        Quantity = (int)detail.Quantity,
                        UnitPrice = detail.UnitPrice
                    });
                }

                if (IsTakeAway(order))
                    await ReserveStockImmediatelyForTakeAwayAsync(order, now);

                order.TotalAmount = total;
                await _orderRepo.AddAsync(order);
                await _uow.CommitAsync();

                return new CreateOrderFromCartResponse
                {
                    OrderId = order.Id,
                    TotalAmount = total,
                    Recipient = ToRecipientSnapshot(order),
                    Items = responseItems,
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

        private async Task<PosCustomerInfo> ResolvePosCustomerAsync(CreatePosOrderRequest request, string operatorUserId)
        {
            if (!string.IsNullOrWhiteSpace(request.CustomerUserId))
            {
                var customerUserId = request.CustomerUserId.Trim();
                var customer = await _userRepo.GetByIdAsync(customerUserId)
                    ?? throw new NotFoundException($"Customer #{customerUserId} không tồn tại");

                if (customer.Id == operatorUserId)
                    throw new InvalidBusinessRuleException("Staff tạo đơn không được đồng thời là customer của đơn POS");

                return new PosCustomerInfo(
                    customer.Id,
                    customer.FullName?.Trim(),
                    customer.PhoneNumber?.Trim(),
                    false);
            }

            var customerPhone = string.IsNullOrWhiteSpace(request.CustomerPhone)
                ? null
                : request.CustomerPhone.Trim();

            var customerName = string.IsNullOrWhiteSpace(request.CustomerName)
                ? null
                : request.CustomerName.Trim();

            if (!string.IsNullOrWhiteSpace(customerPhone))
            {
                // Optional mapping: nếu SĐT đã tồn tại account thì tự gán CustomerUserId.
                var matchedUser = await _userRepo.GetByPhoneAsync(customerPhone);
                if (matchedUser != null)
                {
                    if (matchedUser.Id == operatorUserId)
                        throw new InvalidBusinessRuleException("Staff tạo đơn không được đồng thời là customer của đơn POS");

                    return new PosCustomerInfo(
                        matchedUser.Id,
                        matchedUser.FullName?.Trim(),
                        matchedUser.PhoneNumber?.Trim(),
                        false);
                }

                return new PosCustomerInfo(
                    null,
                    customerName,
                    customerPhone,
                    true);
            }

            return new PosCustomerInfo(
                null,
                null,
                null,
                true);
        }

        private async Task<decimal> ApplyNearExpiryDiscountIfEligibleAsync(
            int productVariantId,
            decimal baseUnitPrice,
            IDictionary<int, (bool IsNearExpiry, decimal EffectivePercent)> eligibilityCache,
            bool includeOfflineOnly = false)
        {
            if (baseUnitPrice <= 0 || _nearExpiryDiscountDays <= 0)
                return baseUnitPrice;

            if (!eligibilityCache.TryGetValue(productVariantId, out var cached))
            {
                var variantMap = await _variantRepo.GetByIdsAsync(new[] { productVariantId });
                var manualPercent = variantMap.TryGetValue(productVariantId, out var pv)
                    ? pv.ManualNearExpiryDiscountPercent
                    : null;

                var availableBoxes = await _boxRepo.GetAvailableBoxesForVariantAsync(
                    productVariantId,
                    includeOfflineOnly);
                var nearestExpiry = availableBoxes
                    .Select(b => b.Lot?.ExpiryDate)
                    .Where(d => d.HasValue)
                    .Select(d => d!.Value)
                    .DefaultIfEmpty(DateTime.MaxValue)
                    .Min();

                var daysLeft = (nearestExpiry - DateTime.UtcNow).TotalDays;
                var isNearExpiry = nearestExpiry != DateTime.MaxValue && daysLeft <= _nearExpiryDiscountDays;

                var effectivePercent = 0m;
                if (isNearExpiry)
                    effectivePercent = manualPercent ?? _nearExpiryDiscountPercent;

                cached = (isNearExpiry, effectivePercent);
                eligibilityCache[productVariantId] = cached;
            }

            if (!cached.IsNearExpiry || cached.EffectivePercent <= 0)
                return baseUnitPrice;

            var discountedPrice = baseUnitPrice * (1 - (cached.EffectivePercent / 100m));
            var safePrice = Math.Round(Math.Max(discountedPrice, 0.01m), 2, MidpointRounding.AwayFromZero);
            return safePrice;
        }

        private static decimal NormalizeCartUnitPricePerKg(decimal storedUnitPrice, decimal boxWeight, decimal? variantPricePerKg)
        {
            // Backward compatibility for old cart rows persisted as "price per box".
            if (variantPricePerKg.HasValue && boxWeight > 0)
            {
                var expectedPricePerBox = variantPricePerKg.Value * boxWeight;
                if (Math.Abs(storedUnitPrice - expectedPricePerBox) <= PriceComparisonTolerance)
                    return variantPricePerKg.Value;
            }

            return storedUnitPrice;
        }

        /// <summary>Sale xác nhận đơn → đơn chuyển sang chờ giữ hàng (allocate).</summary>
        public async Task<SaleConfirmOrderResponseDto> SaleConfirmOrderAsync(int orderId, string confirmedByUserId)
        {
            var order = await _orderRepo.GetByIdWithDetailsAsync(orderId)
                ?? throw new NotFoundException($"Order #{orderId} không tồn tại");

            if (order.Source == OrderSource.POS)
                throw new InvalidBusinessRuleException("Đơn POS không cần bước sale-confirm, có thể allocate ngay.");

            if (order.Status != OrderStatus.PendingSaleConfirmation)
                throw new InvalidBusinessRuleException(
                    $"Chỉ sale được xác nhận khi đơn đang chờ sale (PendingSaleConfirmation). Hiện tại: {order.Status}");

            if (order.Details == null || !order.Details.Any())
                throw new InvalidBusinessRuleException("Order không có chi tiết");

            order.Status = OrderStatus.AwaitingAllocation;
            await _uow.SaveChangesAsync();

            var proposalResult = await AutoProposeAllocationAsync(
                orderId,
                confirmedByUserId,
                skipCustomerOwnershipCheck: true);

            _logger.LogInformation(
                "Order {OrderId} sale-confirmed by {UserId}. Auto-propose result: {ProposalMessage}",
                orderId, confirmedByUserId, proposalResult.Message);

            return new SaleConfirmOrderResponseDto
            {
                Message = $"Sale đã xác nhận đơn. {proposalResult.Message}",
                Order = new OrderListItemDto
                {
                    OrderId = order.Id,
                    TotalAmount = order.TotalAmount,
                    Status = order.Status.ToString(),
                    Source = order.Source.ToString(),
                    CreatedAt = order.CreatedAt,
                    ItemCount = order.Details?.Count ?? 0,
                    LatestPaymentStatus = null
                }
            };
        }

        /// <summary>Khách chọn chờ đủ hàng (backorder) cho phần còn thiếu.</summary>
        public async Task WaitBackorderAsync(int orderId, string userId)
        {
            await WaitBackorderInternalAsync(orderId, userId, skipCustomerOwnershipCheck: false);
        }

        /// <summary>Sales staff chọn chờ backorder thay khách cho phần còn thiếu.</summary>
        public async Task WaitBackorderAsStaffAsync(int orderId, string operatorUserId)
        {
            await WaitBackorderInternalAsync(orderId, operatorUserId, skipCustomerOwnershipCheck: true);
        }

        /// <summary>Khách chấp nhận bỏ phần còn thiếu: chỉ ship phần đã allocate được.</summary>
        public async Task CancelShortageAsync(int orderId, string userId)
        {
            await CancelShortageInternalAsync(orderId, userId, skipCustomerOwnershipCheck: false);
        }

        /// <summary>Sales staff chọn hủy phần thiếu thay khách.</summary>
        public async Task CancelShortageAsStaffAsync(int orderId, string operatorUserId)
        {
            await CancelShortageInternalAsync(orderId, operatorUserId, skipCustomerOwnershipCheck: true);
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
                    p.PaymentStatus == PaymentStatus.Paid ||
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

        public async Task<AllocationProposalResultDto> AutoProposeAllocationAsync(int orderId, string operatorUserId, bool skipCustomerOwnershipCheck = false)
        {
            var order = await _orderRepo.GetByIdWithDetailsAsync(orderId)
                ?? throw new NotFoundException($"Order #{orderId} không tồn tại");

            await ValidateAllocationRequestAsync(order, operatorUserId, skipCustomerOwnershipCheck);

            var existingProposed = await _allocationRepo.GetByOrderIdAsync(orderId, AllocationStatus.Proposed);
            if (existingProposed.Any())
            {
                // Re-check proposal validity to avoid stale "pending warehouse confirm"
                // when another order already reserved the same boxes.
                await _uow.BeginTransactionAsync();
                try
                {
                    var validProposedCount = 0;
                    foreach (var proposal in existingProposed)
                    {
                        var box = await _boxRepo.GetByIdAsync(proposal.BoxId);
                        if (box == null || box.Status != BoxStatus.Stored)
                        {
                            proposal.Status = AllocationStatus.Cancelled;
                            continue;
                        }

                        validProposedCount++;
                    }

                    if (validProposedCount > 0)
                    {
                        if (IsInitialAllocationStatus(order.Status))
                            order.Status = OrderStatus.PendingWarehouseConfirm;

                        await _uow.CommitAsync();
                        return new AllocationProposalResultDto
                        {
                            OrderId = orderId,
                            ProposedBoxCount = validProposedCount,
                            Message = "Đơn đã có đề xuất allocate FEFO hợp lệ, đang chờ kho xác nhận"
                        };
                    }

                    // No valid proposal left -> move out of pending queue before rebuilding.
                    if (order.Status == OrderStatus.PendingWarehouseConfirm)
                        order.Status = OrderStatus.AwaitingAllocation;

                    await _uow.CommitAsync();
                }
                catch
                {
                    await _uow.RollbackAsync();
                    throw;
                }

                existingProposed = new List<OrderAllocation>();
            }

            // Re-read order status after stale-proposal cleanup.
            if (order.Status == OrderStatus.PendingWarehouseConfirm)
            {
                var latestOrder = await _orderRepo.GetByIdWithDetailsAsync(orderId)
                    ?? throw new NotFoundException($"Order #{orderId} không tồn tại");
                order = latestOrder;
            }

            var proposals = await BuildProposedAllocationsAsync(order);
            if (proposals.Count == 0)
            {
                return new AllocationProposalResultDto
                {
                    OrderId = orderId,
                    ProposedBoxCount = 0,
                    Message = "Không có box khả dụng để đề xuất allocate FEFO"
                };
            }

            await _uow.BeginTransactionAsync();
            try
            {
                await _allocationRepo.AddRangeAsync(proposals);

                if (IsInitialAllocationStatus(order.Status))
                    order.Status = OrderStatus.PendingWarehouseConfirm;

                await _uow.CommitAsync();
            }
            catch
            {
                await _uow.RollbackAsync();
                throw;
            }

            return new AllocationProposalResultDto
            {
                OrderId = orderId,
                ProposedBoxCount = proposals.Count,
                Message = "Đã tạo đề xuất allocate FEFO, chờ kho xác nhận"
            };
        }

        public async Task<AllocationProposalResultDto> ReProposeAllocationAsync(int orderId, string operatorUserId, bool skipCustomerOwnershipCheck = false)
        {
            var order = await _orderRepo.GetByIdWithDetailsAsync(orderId)
                ?? throw new NotFoundException($"Order #{orderId} không tồn tại");

            await ValidateAllocationRequestAsync(order, operatorUserId, skipCustomerOwnershipCheck);

            await _uow.BeginTransactionAsync();
            try
            {
                var oldProposals = await _allocationRepo.GetByOrderIdAsync(orderId, AllocationStatus.Proposed);
                foreach (var p in oldProposals)
                    p.Status = AllocationStatus.Cancelled;

                if (order.Status == OrderStatus.PendingWarehouseConfirm)
                    order.Status = OrderStatus.AwaitingAllocation;

                await _uow.CommitAsync();
            }
            catch
            {
                await _uow.RollbackAsync();
                throw;
            }

            return await AutoProposeAllocationAsync(orderId, operatorUserId, skipCustomerOwnershipCheck);
        }

        public async Task<AllocationProposalResultDto> RejectAllocationProposalAsync(int orderId, string operatorUserId, bool skipCustomerOwnershipCheck = false)
        {
            var order = await _orderRepo.GetByIdWithDetailsAsync(orderId)
                ?? throw new NotFoundException($"Order #{orderId} không tồn tại");

            await ValidateAllocationRequestAsync(order, operatorUserId, skipCustomerOwnershipCheck);
            if (order.Status != OrderStatus.PendingWarehouseConfirm)
                throw new InvalidBusinessRuleException(
                    $"Chỉ từ chối proposal khi đơn đang PendingWarehouseConfirm. Hiện tại: {order.Status}");

            await _uow.BeginTransactionAsync();
            try
            {
                var proposals = await _allocationRepo.GetByOrderIdAsync(orderId, AllocationStatus.Proposed);
                foreach (var p in proposals)
                    p.Status = AllocationStatus.Cancelled;

                order.Status = OrderStatus.AwaitingAllocation;
                await _uow.CommitAsync();

                return new AllocationProposalResultDto
                {
                    OrderId = orderId,
                    ProposedBoxCount = 0,
                    Message = "Kho đã từ chối proposal hiện tại. Đơn quay về hàng chờ allocate"
                };
            }
            catch
            {
                await _uow.RollbackAsync();
                throw;
            }
        }

        public async Task<ConfirmAllocationResultDto> ConfirmAllocationAsync(int orderId, string operatorUserId, bool skipCustomerOwnershipCheck = false)
        {
            var order = await _orderRepo.GetByIdWithDetailsAsync(orderId)
                ?? throw new NotFoundException($"Order #{orderId} không tồn tại");

            await ValidateAllocationRequestAsync(order, operatorUserId, skipCustomerOwnershipCheck);

            var proposedAllocations = await _allocationRepo.GetByOrderIdAsync(orderId, AllocationStatus.Proposed);
            // Race-condition / stale proposal handling:
            // Another order may have reserved boxes, causing all proposals to be cancelled by auto-propose cleanup
            // or leaving no Proposed rows when staff clicks confirm.
            // In that case, treat confirm as "reserved 0", compute shortage, and require customer decision.
            if (!proposedAllocations.Any())
            {
                await _uow.BeginTransactionAsync();
                try
                {
                    var originalStatus = order.Status;

                    // No new boxes reserved. For initial allocation, fulfilled=0, shortage=quantity.
                    // For backorder, keep existing fulfilled/shortage (no progress this round).
                    if (IsInitialAllocationStatus(originalStatus))
                    {
                        foreach (var detail in order.Details)
                        {
                            detail.FulfilledQuantity = 0;
                            detail.ShortageQuantity = detail.Quantity;
                        }

                        order.TotalAmount = 0m;
                    }

                    order.Status = IsOrderFullyReserved(order)
                        ? OrderStatus.Confirmed
                        : (originalStatus == OrderStatus.BackorderWaiting
                            ? OrderStatus.BackorderWaiting
                            : OrderStatus.PartiallyAllocated);

                    await _uow.CommitAsync();

                    var fulfilledQty = order.Details.Sum(d => d.FulfilledQuantity);
                    var shortageQty = order.Details.Sum(d => d.ShortageQuantity);
                    var needsCustomerAction =
                        order.Status == OrderStatus.PartiallyAllocated && shortageQty > 0;

                    if (needsCustomerAction)
                    {
                        try
                        {
                            await _notificationService.NotifyOrderAllocationShortageAsync(order.Id);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(
                                ex,
                                "Allocation confirm had no proposals for order {OrderId}, customer notification failed",
                                order.Id);
                        }
                    }

                    return new ConfirmAllocationResultDto
                    {
                        OrderId = order.Id,
                        Status = order.Status.ToString(),
                        FulfilledQuantity = fulfilledQty,
                        ShortageQuantity = shortageQty,
                        CustomerActionRequired = needsCustomerAction,
                        CustomerActions = needsCustomerAction
                            ? (fulfilledQty > 0
                                ? new List<string> { "wait_backorder", "cancel_shortage" }
                                : new List<string> { "wait_backorder", "cancel_order" })
                            : new List<string>(),
                        Message = needsCustomerAction
                            ? (fulfilledQty > 0
                                ? "Đơn thiếu hàng sau khi xác nhận kho. Vui lòng để khách chọn chờ backorder hoặc hủy phần thiếu."
                                : "Đơn thiếu hàng sau khi xác nhận kho nhưng chưa giữ được phần nào. Vui lòng để khách chọn chờ backorder hoặc hủy đơn.")
                            : "Kho đã xác nhận allocate thành công"
                    };
                }
                catch
                {
                    await _uow.RollbackAsync();
                    throw;
                }
            }

            await _uow.BeginTransactionAsync();
            try
            {
                var originalStatus = order.Status;
                var now = DateTime.UtcNow;
                var expiredAt = now.AddHours(AllocationExpirationHours);
                var totalAmount = 0m;
                var allocatedCountByDetail = new Dictionary<int, int>();

                foreach (var proposal in proposedAllocations)
                {
                    var box = await _boxRepo.GetByIdAsync(proposal.BoxId);
                    if (box == null || box.Status != BoxStatus.Stored)
                    {
                        proposal.Status = AllocationStatus.Cancelled;
                        continue;
                    }

                    proposal.Status = AllocationStatus.Reserved;
                    proposal.ReservedAt = now;
                    proposal.ExpiredAt = expiredAt;

                    box.Status = BoxStatus.Reserved;
                    await _boxRepo.UpdateAsync(box);

                    if (!allocatedCountByDetail.ContainsKey(proposal.OrderDetailId))
                        allocatedCountByDetail[proposal.OrderDetailId] = 0;
                    allocatedCountByDetail[proposal.OrderDetailId]++;
                }

                foreach (var detail in order.Details)
                {
                    allocatedCountByDetail.TryGetValue(detail.Id, out var allocated);

                    var prevFulfilled = detail.FulfilledQuantity;
                    var prevShortage = detail.ShortageQuantity;
                    var boxesNeeded = IsInitialAllocationStatus(originalStatus)
                        ? (int)detail.Quantity
                        : (int)detail.ShortageQuantity;

                    if (IsInitialAllocationStatus(originalStatus))
                    {
                        detail.FulfilledQuantity = allocated;
                        detail.ShortageQuantity = boxesNeeded - allocated;
                    }
                    else
                    {
                        detail.FulfilledQuantity = prevFulfilled + allocated;
                        detail.ShortageQuantity = Math.Max(0, prevShortage - allocated);
                    }

                    totalAmount += allocated * detail.BoxWeight * detail.UnitPrice;
                }

                order.TotalAmount = IsInitialAllocationStatus(originalStatus)
                    ? totalAmount
                    : order.TotalAmount + totalAmount;

                order.Status = IsOrderFullyReserved(order)
                    ? OrderStatus.Confirmed
                    : (originalStatus == OrderStatus.BackorderWaiting
                        ? OrderStatus.BackorderWaiting
                        : OrderStatus.PartiallyAllocated);

                await _uow.CommitAsync();

                var fulfilledQty = order.Details.Sum(d => d.FulfilledQuantity);
                var shortageQty = order.Details.Sum(d => d.ShortageQuantity);
                var needsCustomerAction =
                    order.Status == OrderStatus.PartiallyAllocated && shortageQty > 0;

                if (needsCustomerAction)
                {
                    try
                    {
                        await _notificationService.NotifyOrderAllocationShortageAsync(order.Id);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(
                            ex,
                            "Allocation confirmed with shortage for order {OrderId}, but customer notification failed",
                            order.Id);
                    }
                }

                return new ConfirmAllocationResultDto
                {
                    OrderId = order.Id,
                    Status = order.Status.ToString(),
                    FulfilledQuantity = fulfilledQty,
                    ShortageQuantity = shortageQty,
                    CustomerActionRequired = needsCustomerAction,
                    CustomerActions = needsCustomerAction
                        ? (fulfilledQty > 0
                            ? new List<string> { "wait_backorder", "cancel_shortage" }
                            : new List<string> { "wait_backorder", "cancel_order" })
                        : new List<string>(),
                    Message = needsCustomerAction
                        ? (fulfilledQty > 0
                            ? "Đơn thiếu hàng sau khi xác nhận kho. Vui lòng để khách chọn chờ backorder hoặc hủy phần thiếu."
                            : "Đơn thiếu hàng sau khi xác nhận kho nhưng chưa giữ được phần nào. Vui lòng để khách chọn chờ backorder hoặc hủy đơn.")
                        : "Kho đã xác nhận allocate thành công"
                };
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

        /// <summary>Tương thích endpoint cũ: propose FEFO rồi xác nhận ngay.</summary>
        public async Task ConfirmOrderAsync(int orderId, string operatorUserId, bool skipCustomerOwnershipCheck = false)
        {
            await AutoProposeAllocationAsync(orderId, operatorUserId, skipCustomerOwnershipCheck);
            await ConfirmAllocationAsync(orderId, operatorUserId, skipCustomerOwnershipCheck);
        }

        private async Task WaitBackorderInternalAsync(int orderId, string actorUserId, bool skipCustomerOwnershipCheck)
        {
            var order = await _orderRepo.GetByIdWithDetailsAndPaymentsAsync(orderId)
                ?? throw new NotFoundException($"Order #{orderId} không tồn tại");

            if (!IsDelivery(order))
                throw new InvalidBusinessRuleException("Backorder chỉ áp dụng cho đơn Delivery");

            if (!skipCustomerOwnershipCheck && order.UserId != actorUserId)
                throw new ForbiddenException("Bạn không có quyền thao tác trên đơn hàng này");

            if (order.Status != OrderStatus.PartiallyAllocated)
                throw new InvalidBusinessRuleException(
                    $"Chỉ có thể chờ backorder khi đơn đang ở trạng thái PartiallyAllocated. Hiện tại: {order.Status}");

            if (order.Details == null || !order.Details.Any(d => d.ShortageQuantity > 0))
                throw new InvalidBusinessRuleException("Đơn không còn thiếu hàng để chờ backorder");

            if (order.Payments != null && order.Payments.Any(p =>
                    p.PaymentStatus == PaymentStatus.Paid ||
                    p.PaymentStatus == PaymentStatus.Refunded))
            {
                throw new InvalidBusinessRuleException("Không thể backorder: đơn đã thanh toán thành công");
            }

            // Cho phép vào backorder ngay cả khi chưa giữ được box nào (reserved = 0),
            // miễn là đơn vẫn còn shortage để chờ allocate trong các vòng backorder sau.
            order.Status = OrderStatus.BackorderWaiting;
            order.BackorderExpiryNotifiedAt = null;
            await _uow.SaveChangesAsync();
        }

        private async Task CancelShortageInternalAsync(int orderId, string actorUserId, bool skipCustomerOwnershipCheck)
        {
            var order = await _orderRepo.GetByIdWithDetailsAndPaymentsAsync(orderId)
                ?? throw new NotFoundException($"Order #{orderId} không tồn tại");

            if (!IsDelivery(order))
                throw new InvalidBusinessRuleException("Cancel shortage chỉ áp dụng cho đơn Delivery");

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
                || order.Status == OrderStatus.PendingWarehouseConfirm
                || order.Status == OrderStatus.PartiallyAllocated
                || order.Status == OrderStatus.AwaitingPayment
                || order.Status == OrderStatus.BackorderWaiting
                || order.Status == OrderStatus.InventoryFailed;

            if (!canCancelBeforeAllocation)
                throw new InvalidBusinessRuleException(
                    $"Chỉ có thể hủy đơn khi chưa vào bước shipping. Hiện tại: {order.Status}");

            if (order.Payments != null && order.Payments.Any(p =>
                    p.PaymentStatus == PaymentStatus.Paid ||
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

        private static bool CheckCanComplete(Order order, bool hasPendingComplaint, DateTime nowUtc)
        {
            if (order.Status != OrderStatus.Delivered)
                return false;

            if (!order.DeliveredAt.HasValue)
                return false;

            var completeAt = order.DeliveredAt.Value.AddDays(CompleteAfterDeliveredDays);
            if (nowUtc < completeAt)
                return false;

            return !hasPendingComplaint;
        }

        private static bool IsInitialAllocationStatus(OrderStatus status) =>
            status == OrderStatus.AwaitingAllocation
            || status == OrderStatus.AwaitingPayment
            || status == OrderStatus.PendingWarehouseConfirm;

        private async Task ValidateAllocationRequestAsync(Order order, string operatorUserId, bool skipCustomerOwnershipCheck)
        {
            if (!skipCustomerOwnershipCheck && order.UserId != operatorUserId)
                throw new ForbiddenException("Bạn không có quyền thao tác trên đơn hàng này");

            if (!IsDelivery(order))
                throw new InvalidBusinessRuleException("Flow allocation nâng cao chỉ áp dụng cho đơn Delivery");

            var canAllocate =
                order.Status == OrderStatus.AwaitingAllocation
                || order.Status == OrderStatus.PendingWarehouseConfirm
                || order.Status == OrderStatus.AwaitingPayment
                || order.Status == OrderStatus.PartiallyAllocated
                || order.Status == OrderStatus.BackorderWaiting;

            if (!canAllocate)
                throw new InvalidBusinessRuleException(
                    $"Chỉ có thể giữ hàng khi đơn đang chờ giữ hàng / backorder. Hiện tại: {order.Status}");

            if (order.Details == null || !order.Details.Any())
                throw new InvalidBusinessRuleException("Order không có chi tiết");

            if (order.Status == OrderStatus.BackorderWaiting)
            {
                var deadline = await GetBackorderDeadlineAtAsync(order.Id);
                if (!deadline.HasValue || DateTime.UtcNow > deadline.Value)
                    throw new InvalidBusinessRuleException(
                        "Backorder đã hết thời gian chờ. Vui lòng gọi endpoint xử lý timeout (cancel-shortage hoặc cancel-order) để tiếp tục.");
            }
        }

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

        private async Task<List<OrderAllocation>> BuildProposedAllocationsAsync(Order order)
        {
            var now = DateTime.UtcNow;
            var allocations = new List<OrderAllocation>();
            var selectedBoxIds = new HashSet<int>();

            var isInitialAllocation = IsInitialAllocationStatus(order.Status);
            var existingAllocations = await _allocationRepo.GetByOrderIdAsync(order.Id);
            var activeAllocationStatuses = new HashSet<AllocationStatus>
            {
                AllocationStatus.Proposed,
                AllocationStatus.Reserved,
                AllocationStatus.Picked
            };
            var allocatedBoxByDetail = existingAllocations
                .Where(x => activeAllocationStatuses.Contains(x.Status))
                .GroupBy(x => x.OrderDetailId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(x => x.BoxId).ToHashSet());

            foreach (var detail in order.Details)
            {
                var boxesNeeded = isInitialAllocation
                    ? (int)detail.Quantity
                    : (int)detail.ShortageQuantity;

                if (boxesNeeded <= 0)
                    continue;

                var includeOfflineOnly = order.Source == OrderSource.POS;
                var boxes = await _boxRepo.GetAvailableBoxesForVariantAsync(
                    detail.ProductVariantId,
                    includeOfflineOnly);
                boxes = boxes
                    .Where(b =>
                        b.IsPartial == detail.IsPartial
                        && b.Weight == detail.BoxWeight
                        && !selectedBoxIds.Contains(b.Id)
                        && (!allocatedBoxByDetail.ContainsKey(detail.Id) || !allocatedBoxByDetail[detail.Id].Contains(b.Id)))
                    .ToList();

                var allocated = 0;
                foreach (var box in boxes)
                {
                    if (allocated >= boxesNeeded) break;
                    selectedBoxIds.Add(box.Id);

                    allocations.Add(new OrderAllocation
                    {
                        OrderId = order.Id,
                        OrderDetailId = detail.Id,
                        BoxId = box.Id,
                        ReservedQuantity = box.Weight,
                        Status = AllocationStatus.Proposed,
                        ReservedAt = now
                    });
                    allocated++;
                }
            }

            return allocations;
        }

        private async Task ReserveStockImmediatelyForTakeAwayAsync(Order order, DateTime nowUtc)
        {
            foreach (var detail in order.Details)
            {
                var neededBoxes = (int)detail.Quantity;
                if (neededBoxes <= 0)
                    continue;

                var availableBoxes = await _boxRepo.GetAvailableBoxesForVariantAsync(
                    detail.ProductVariantId,
                    includeOfflineOnly: true);

                var selectedBoxes = availableBoxes
                    .Where(b => b.IsPartial == detail.IsPartial && b.Weight == detail.BoxWeight)
                    .OrderBy(b => b.Lot?.ExpiryDate ?? DateTime.MaxValue)
                    .ThenBy(b => b.CreatedAt)
                    .Take(neededBoxes)
                    .ToList();

                if (selectedBoxes.Count < neededBoxes)
                    throw new InvalidBusinessRuleException(
                        $"Không đủ tồn kho để giữ hàng ngay tại quầy cho biến thể #{detail.ProductVariantId}");

                foreach (var box in selectedBoxes)
                {
                    box.Status = BoxStatus.Reserved;
                    await _boxRepo.UpdateAsync(box);

                    order.Allocations.Add(new OrderAllocation
                    {
                        Order = order,
                        OrderDetail = detail,
                        BoxId = box.Id,
                        ReservedQuantity = box.Weight,
                        Status = AllocationStatus.Reserved,
                        ReservedAt = nowUtc,
                        ExpiredAt = nowUtc.AddHours(AllocationExpirationHours)
                    });
                }

                detail.FulfilledQuantity = detail.Quantity;
                detail.ShortageQuantity = 0;
            }
        }

        private static void ApplyRecipientToOrder(Order order, OrderRecipientCheckoutDto recipient)
        {
            order.RecipientFullName = recipient.FullName.Trim();
            order.RecipientPhone = recipient.Phone.Trim();
            order.RecipientAddress = recipient.Address.Trim();
        }

        private static OrderRecipientSnapshotDto ToRecipientSnapshot(Order order) =>
            new()
            {
                FullName = order.RecipientFullName,
                Phone = order.RecipientPhone,
                Address = order.RecipientAddress
            };

        private sealed record PosCustomerInfo(
            string? CustomerUserId,
            string? CustomerName,
            string? CustomerPhone,
            bool IsGuest);

        private static bool IsTakeAway(Order order) => order.FulfillmentType == FulfillmentType.TakeAway;
        private static bool IsDelivery(Order order) => order.FulfillmentType == FulfillmentType.Delivery;

    }
}
