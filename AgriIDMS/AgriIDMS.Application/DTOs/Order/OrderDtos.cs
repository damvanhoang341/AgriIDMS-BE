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
        public decimal LineAmount => Quantity * UnitPrice;
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
