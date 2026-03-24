using System;
using System.Collections.Generic;

namespace AgriIDMS.Application.DTOs.Order
{
    /// <summary>Dòng sản phẩm trong đơn (hiển thị sau khi tạo đơn từ giỏ).</summary>
    public class OrderItemDto
    {
        public int ProductVariantId { get; set; }
        public string ProductName { get; set; } = null!;
        public string Grade { get; set; } = null!;
        /// <summary>Trọng lượng mỗi box (kg).</summary>
        public decimal BoxWeight { get; set; }
        /// <summary>Box lẻ (partial) hay box đầy.</summary>
        public bool IsPartial { get; set; }
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal LineAmount => Quantity * BoxWeight * UnitPrice;
    }

    /// <summary>Response khi tạo đơn hàng từ giỏ: OrderId + danh sách items.</summary>
    public class CreateOrderFromCartResponse
    {
        public int OrderId { get; set; }
        public decimal TotalAmount { get; set; }
        public IList<OrderItemDto> Items { get; set; } = new List<OrderItemDto>();
        /// <summary>True chỉ sau khi gọi allocate thành công (đơn → Confirmed). Sau khi tạo đơn luôn false.</summary>
        public bool AllocationSucceeded { get; set; }
        /// <summary>Thông báo lỗi khi allocate (thiếu hàng, v.v.).</summary>
        public string? AllocationMessage { get; set; }
    }

    public class CreateOrderFromCartRequest
    {
        public List<CreateOrderFromCartByVariantIdsRequest> Items { get; set; } = new();
    }

    public class CreateOrderFromCartByVariantIdsRequest
    {
        public int ProductVariantId { get; set; }
        public decimal BoxWeight { get; set; }
        public bool IsPartial { get; set; }
        public int Quantity { get; set; }
    }

    public class CreatePosOrderRequest
    {
        /// <summary>Khách hàng gắn với đơn POS (optional). Nếu null sẽ dùng chính staff tạo đơn.</summary>
        public string? CustomerUserId { get; set; }
        public List<CreatePosOrderItemRequest> Items { get; set; } = new();
    }

    public class CreatePosOrderItemRequest
    {
        public int ProductVariantId { get; set; }
        public decimal BoxWeight { get; set; }
        public bool IsPartial { get; set; }
        public int Quantity { get; set; }
        /// <summary>Đơn giá bán tại quầy. Nếu null hệ thống lấy theo giá variant hiện tại.</summary>
        public decimal? UnitPrice { get; set; }
    }

    public class GetOrdersQuery
    {
        public string? Status { get; set; }
    }

    public class GetPendingSaleConfirmOrdersQuery
    {
        public string? CustomerUserId { get; set; }
        public int Skip { get; set; } = 0;
        public int Take { get; set; } = 50;
    }

    public class GetPendingAllocationOrdersQuery
    {
        public string? CustomerUserId { get; set; }
        public string? Source { get; set; } // Online | POS
        public int Skip { get; set; } = 0;
        public int Take { get; set; } = 50;
    }

    public class AllocationProposalResultDto
    {
        public int OrderId { get; set; }
        public int ProposedBoxCount { get; set; }
        public bool ProposedEmpty => ProposedBoxCount <= 0;
        public string Message { get; set; } = null!;
    }

    public class ConfirmAllocationResultDto
    {
        public int OrderId { get; set; }
        public string Status { get; set; } = null!;
        public decimal FulfilledQuantity { get; set; }
        public decimal ShortageQuantity { get; set; }
        public bool CustomerActionRequired { get; set; }
        public IList<string> CustomerActions { get; set; } = new List<string>();
        public string Message { get; set; } = null!;
    }

    public class AllocationProposalItemDto
    {
        public int AllocationId { get; set; }
        public int OrderDetailId { get; set; }
        public int ProductVariantId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string Grade { get; set; } = string.Empty;
        public int BoxId { get; set; }
        public string BoxCode { get; set; } = string.Empty;
        public decimal BoxWeight { get; set; }
        public bool IsPartial { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public string Status { get; set; } = null!;
    }

    public class AllocationProposalDetailSummaryDto
    {
        public int OrderDetailId { get; set; }
        public int ProductVariantId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string Grade { get; set; } = string.Empty;
        public decimal BoxWeight { get; set; }
        public bool IsPartial { get; set; }
        public int RequestedQuantity { get; set; }
        public int ProposedQuantity { get; set; }
        public int ShortageQuantity { get; set; }
        public bool IsSufficient => ShortageQuantity <= 0;
    }

    public class AllocationProposalOverviewDto
    {
        public int OrderId { get; set; }
        public string OrderStatus { get; set; } = null!;
        public int TotalRequestedBoxes { get; set; }
        public int TotalProposedBoxes { get; set; }
        public int TotalShortageBoxes { get; set; }
        public bool IsFullyProposed => TotalShortageBoxes <= 0;
        public IList<AllocationProposalDetailSummaryDto> Details { get; set; } = new List<AllocationProposalDetailSummaryDto>();
        public IList<AllocationProposalItemDto> Proposals { get; set; } = new List<AllocationProposalItemDto>();
    }

    public class OrderListItemDto
    {
        public int OrderId { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = null!;
        public string Source { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public int ItemCount { get; set; }
        public string? LatestPaymentStatus { get; set; }
    }

    public class SaleConfirmOrderResponseDto
    {
        public string Message { get; set; } = null!;
        public OrderListItemDto Order { get; set; } = null!;
    }

    public class OrderDetailDto
    {
        public int OrderId { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = null!;
        public string Source { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public string? LatestPaymentStatus { get; set; }
        public IList<OrderDetailItemDto> Items { get; set; } = new List<OrderDetailItemDto>();
    }

    public class OrderDetailItemDto
    {
        public int ProductVariantId { get; set; }
        public string ProductName { get; set; } = null!;
        public string Grade { get; set; } = null!;
        public decimal BoxWeight { get; set; }
        public bool IsPartial { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal FulfilledQuantity { get; set; }
        public decimal ShortageQuantity { get; set; }
    }

    /// <summary>Hành động cần làm khi hết thời gian backorder.</summary>
    public enum BackorderExpiredAction
    {
        CancelShortage = 0,
        CancelOrder = 1
    }

    public class BackorderAllocateRequestDto
    {
        public BackorderExpiredAction ExpiredAction { get; set; }
            = BackorderExpiredAction.CancelShortage;
    }

    public class OverdueBackorderItemDto
    {
        public int OrderId { get; set; }
        public string CustomerUserId { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public DateTime BackorderDeadlineAt { get; set; }
        public decimal TotalShortageQuantity { get; set; }
        public decimal TotalReservedQuantity { get; set; }
        public decimal CurrentTotalAmount { get; set; }
        public string Status { get; set; } = null!;
    }
}
