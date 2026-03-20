using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgriIDMS.Domain.Enums
{
    public enum OrderStatus
    {
        /// <summary>Đơn cũ: chờ thanh toán / giữ hàng trực tiếp. Đơn mới không còn dùng làm bước đầu.</summary>
        AwaitingPayment = 0,
        Paid = 1,
        Confirmed = 2,
        InventoryFailed = 3,
        Shipping = 4,
        Completed = 5,
        Cancelled = 6,
        Refunded = 7,

        /// <summary>Vừa tạo từ giỏ — chờ sale xác nhận trước khi được giữ hàng.</summary>
        PendingSaleConfirmation = 8,

        /// <summary>Sale đã xác nhận — chờ bước allocate (giữ tồn).</summary>
        AwaitingAllocation = 9
    }

    public enum PaymentStatus
    {
        Pending = 0,      // vừa tạo, chưa thanh toán COD POS
        Processing = 1,   // đang chờ gateway xử lý POS
        Success = 2,      // thanh toán thành công COD POS
        Failed = 3,       // thanh toán thất bại
        Cancelled = 4,    // user huỷ
        Refunded = 5      // hoàn tiền
    }

    public enum PaymentMethod
    {
        COD = 0,
        VNPay = 1,
        Momo = 2,
        Banking = 3
    }
    public enum RefundStatus
    {
        Pending = 0,
        Processing = 1,
        Success = 2,
        Failed = 3,
        ManualReview = 4
    }

    public enum AllocationStatus
    {
        Reserved,
        Picked,
        Cancelled
    }
}
