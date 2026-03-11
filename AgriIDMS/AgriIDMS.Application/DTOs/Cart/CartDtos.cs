using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AgriIDMS.Application.DTOs.Cart
{
    public class AddCartItemRequest
    {
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "ProductVariantId không hợp lệ")]
        public int ProductVariantId { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Số lượng box phải >= 1")]
        public int Quantity { get; set; }
    }

    public class CartItemDto
    {
        public int ProductVariantId { get; set; }
        public string ProductName { get; set; } = null!;
        public string Grade { get; set; } = null!;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal LineAmount => Quantity * UnitPrice;
    }

    public class CartDto
    {
        public IList<CartItemDto> Items { get; set; } = new List<CartItemDto>();
        public decimal TotalAmount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}

