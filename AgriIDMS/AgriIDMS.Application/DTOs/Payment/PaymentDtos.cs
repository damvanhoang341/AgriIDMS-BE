using AgriIDMS.Domain.Enums;
using System;
using System.ComponentModel.DataAnnotations;

namespace AgriIDMS.Application.DTOs.Payment
{
    public class CreatePaymentRequest
    {
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "OrderId phải lớn hơn 0")]
        public int OrderId { get; set; }

        [Required]
        [EnumDataType(typeof(PaymentMethod), ErrorMessage = "PaymentMethod không hợp lệ")]
        public PaymentMethod PaymentMethod { get; set; }
    }

    public class PaymentResponseDto
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public string PaymentMethod { get; set; } = null!;
        public string PaymentStatus { get; set; } = null!;
        public string? TransactionCode { get; set; }
        public decimal Amount { get; set; }
        public DateTime? PaidAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? CheckoutUrl { get; set; }
    }
}
