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
        /// <summary>True nếu auto-allocate thành công (order đã Confirmed).</summary>
        public bool AllocationSucceeded { get; set; }
        /// <summary>Thông báo khi allocate thất bại (thiếu hàng, v.v.).</summary>
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
}
