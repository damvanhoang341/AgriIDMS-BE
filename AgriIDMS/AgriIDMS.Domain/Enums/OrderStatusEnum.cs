using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgriIDMS.Domain.Enums
{
    public enum OrderStatus
    {
        AwaitingPayment = 0,
        Paid = 1,
        Confirmed = 2,
        InventoryFailed = 3,
        Shipping = 4,
        Completed = 5,
        Cancelled = 6,
        Refunded = 7
    }

    public enum PaymentStatus
    {
        Pending = 0,      // vừa tạo, chưa thanh toán
        Processing = 1,   // đang chờ gateway xử lý
        Success = 2,      // thanh toán thành công
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
