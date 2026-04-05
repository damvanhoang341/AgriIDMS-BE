using AgriIDMS.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgriIDMS.Domain.Entities
{
    public class Order
    {
        public int Id { get; set; }

        public string UserId { get; set; }

        public DateTime CreatedAt { get; set; }

        public decimal TotalAmount { get; set; }

        public OrderSource Source { get; set; } = OrderSource.Online;
        public FulfillmentType FulfillmentType { get; set; } = FulfillmentType.Delivery;

        /// <summary>
        /// Trả trước / trả sau. Đơn online: null cho đến khi khách chọn (sau khi sale xác nhận).
        /// POS TakeAway: PayBefore. POS Delivery: staff chọn khi tạo đơn (PayBefore hoặc PayAfter).
        /// </summary>
        public PaymentTiming? PaymentTiming { get; set; }

        /// <summary>
        /// POS TakeAway: bản ghi mới luôn <see cref="PosCheckoutTiming.PayBeforePick"/>; null = bản ghi cũ.
        /// </summary>
        public PosCheckoutTiming? PosCheckoutTiming { get; set; }

        public OrderStatus Status { get; set; }

        /// <summary>TakeAway: luôn <see cref="ShippingStatus.None"/>. Delivery: các bước shipper / giao.</summary>
        public ShippingStatus ShippingStatus { get; set; } = ShippingStatus.None;

        public DateTime? DeliveredAt { get; set; }
        public DateTime? BackorderExpiryNotifiedAt { get; set; }

        /// <summary>
        /// Đơn online PayBefore: đã gửi thông báo cho sale khi quá hạn giữ hàng (allocation ExpiredAt) mà chưa thanh toán.
        /// </summary>
        public DateTime? PaymentDeadlineNotifiedAt { get; set; }
        public string? CustomerUserId { get; set; }
        public string? CustomerName { get; set; }
        public string? CustomerPhone { get; set; }
        public bool IsGuest { get; set; }

        /// <summary>Snapshot người nhận khi đặt online (có thể khác profile tài khoản).</summary>
        public string RecipientFullName { get; set; } = string.Empty;

        public string RecipientPhone { get; set; } = string.Empty;

        public string RecipientAddress { get; set; } = string.Empty;

        public ICollection<OrderDetail> Details { get; set; } = new List<OrderDetail>();
        public ICollection<Payment> Payments { get; set; } = new List<Payment>();
        public ICollection<OrderAllocation> Allocations { get; set; } = new List<OrderAllocation>();
        public ICollection<ExportReceipt> ExportReceipts { get; set; } = new List<ExportReceipt>();
    }
}
