using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgriIDMS.Domain.Enums
{
    public enum OrderSource
    {
        Online = 0,
        POS = 1
    }

    public enum FulfillmentType
    {
        TakeAway = 0,
        Delivery = 1
    }

    /// <summary>
    /// Thứ tự thanh toán vs pick/xuất kho cho đơn POS mang về (TakeAway).
    /// </summary>
    public enum PosCheckoutTiming
    {
        /// <summary>Pick/xuất kho (hoặc chuẩn bị hàng) trước, thu tiền sau — mặc định, tương thích đơn cũ.</summary>
        PickBeforePay = 0,

        /// <summary>Thu tiền (đã Paid) trước, sau đó mới được tạo phiếu xuất/pick; hoàn tất khi duyệt xuất.</summary>
        PayBeforePick = 1
    }

    public enum OrderStatus
    {
        /// <summary>Đơn cũ: chờ thanh toán / giữ hàng trực tiếp. Đơn mới không còn dùng làm bước đầu.</summary>
        AwaitingPayment = 0,
        Confirmed = 2,
        InventoryFailed = 3,
        Shipping = 4,
        Completed = 5,
        Cancelled = 6,
        Refunded = 7,

        /// <summary>Vừa tạo từ giỏ — chờ sale xác nhận trước khi được giữ hàng.</summary>
        PendingSaleConfirmation = 8,

        /// <summary>Sale đã xác nhận — chờ bước allocate (giữ tồn).</summary>
        AwaitingAllocation = 9,

        /// <summary>Hệ thống đã đề xuất allocate FEFO, chờ kho xác nhận giữ hàng.</summary>
        PendingWarehouseConfirm = 12,

        /// <summary>Đã allocate/giữ được một phần, còn thiếu hàng.</summary>
        PartiallyAllocated = 10,

        /// <summary>Khách chọn chờ backorder phần còn thiếu (đến hạn thì xử lý theo policy).</summary>
        BackorderWaiting = 11,

        /// <summary>Đơn đã giao thành công đến khách hàng (mốc bắt đầu cho customer review).</summary>
        Delivered = 13,

        /// <summary>Giao hàng thất bại (shipper không giao được).</summary>
        FailedDelivery = 14,

        /// <summary>Đơn bị hoàn trả sau giao thất bại hoặc khách từ chối nhận.</summary>
        Returned = 15
    }

    public enum PaymentStatus
    {
        Pending = 0,      // vừa tạo, chưa thanh toán COD POS
        Processing = 1,   // đang chờ gateway xử lý POS
        Paid = 2,         // thanh toán thành công COD POS
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
        Proposed,
        Reserved,
        Picked,
        Cancelled,

        /// <summary>Tạm giữ box khi khách vừa đặt đơn online; chưa chiếm trạng thái kho cứng. Hết hạn thì box lại khả dụng cho đơn khác.</summary>
        SoftLocked
    }
}
