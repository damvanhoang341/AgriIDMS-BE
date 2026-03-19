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
    }

    public class CreateOrderFromCartRequest
    {
        public List<CreateOrderFromCartByVariantIdsRequest> Items { get; set; } = new();
    }

    public class CreateOrderFromCartByVariantIdsRequest
    {
        public int ProductVariantId { get; set; }
        public int Quantity { get; set; }
    }
}
