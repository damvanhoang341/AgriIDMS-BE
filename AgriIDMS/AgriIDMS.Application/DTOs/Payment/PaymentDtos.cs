using AgriIDMS.Application.Serialization;
using AgriIDMS.Domain.Enums;
using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AgriIDMS.Application.DTOs.Payment
{
    public class CreatePaymentRequest
    {
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "OrderId phải lớn hơn 0")]
        public int OrderId { get; set; }

        /// <summary>
        /// <see cref="PaymentMethod.Cash"/> = tiền mặt; <see cref="PaymentMethod.Banking"/> = QR / PayOS.
        /// JSON có thể gửi "COD" (legacy) — được map sang Cash.
        /// </summary>
        [Required]
        [JsonConverter(typeof(PaymentMethodCompatJsonConverter))]
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

    public class GetPendingCashPaymentsQuery
    {
        public int? OrderId { get; set; }
        public string? CustomerUserId { get; set; }
        public int Skip { get; set; } = 0;
        public int Take { get; set; } = 50;
    }

    public class PendingCashPaymentItemDto
    {
        public int PaymentId { get; set; }
        public int OrderId { get; set; }
        public string CustomerUserId { get; set; } = null!;
        /// <summary>Tên khách (POS / guest) hoặc người nhận — ưu tiên CustomerName.</summary>
        public string? CustomerName { get; set; }
        /// <summary>SĐT khách — ưu tiên CustomerPhone.</summary>
        public string? CustomerPhone { get; set; }
        public decimal Amount { get; set; }
        public string PaymentStatus { get; set; } = null!;
        public string PaymentMethod { get; set; } = null!;
        public string OrderStatus { get; set; } = null!;
        /// <summary>Nguồn đơn (Online / POS) — dùng cho UI (ví dụ xác nhận tiền mặt tại quầy).</summary>
        public string OrderSource { get; set; } = string.Empty;
        public string FulfillmentType { get; set; } = string.Empty;
        public string? PaymentTiming { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
