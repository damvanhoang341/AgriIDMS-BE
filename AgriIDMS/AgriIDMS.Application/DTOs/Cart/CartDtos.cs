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

        /// <summary>Trọng lượng mỗi box (kg) mà customer chọn.</summary>
        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "BoxWeight phải > 0")]
        public decimal BoxWeight { get; set; }

        /// <summary>Box lẻ (partial) hay box đầy.</summary>
        [Required]
        public bool IsPartial { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Số lượng box phải >= 1")]
        public int Quantity { get; set; }
    }

    public class UpdateCartItemRequest
    {
        /// <summary>Trọng lượng mỗi box (kg) mà customer chọn.</summary>
        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "BoxWeight phải > 0")]
        public decimal BoxWeight { get; set; }

        /// <summary>Box lẻ (partial) hay box đầy.</summary>
        [Required]
        public bool IsPartial { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Số lượng box phải >= 1")]
        public int Quantity { get; set; }
    }

    /// <summary>Thông tin sản phẩm trong giỏ để hiển thị cho khách hàng.</summary>
    public class CartItemDto
    {
        public int ProductVariantId { get; set; }
        public string ProductVariantName { get; set; } = null!;
        public string ProductName { get; set; } = null!;
        public string Grade { get; set; } = null!;
        public string? ImageUrl { get; set; }
        public int Quantity { get; set; }
        /// <summary>Trọng lượng mỗi box (kg).</summary>
        public decimal BoxWeight { get; set; }
        /// <summary>Box lẻ (partial) hay box đầy.</summary>
        public bool IsPartial { get; set; }
        public decimal UnitPrice { get; set; }
        /// <summary>Thành tiền dòng = số box * đơn giá (kg) * trọng lượng mỗi box.</summary>
        public decimal LineAmount => Quantity * UnitPrice * BoxWeight;
    }

    public class CartDto
    {
        public IList<CartItemDto> Items { get; set; } = new List<CartItemDto>();
        public decimal TotalAmount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
