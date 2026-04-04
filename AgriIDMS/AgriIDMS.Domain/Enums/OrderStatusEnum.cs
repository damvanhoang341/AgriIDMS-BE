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
        /// <summary>Chưa quyết toán (tiền mặt chờ xác nhận thu, hoặc chưa hoàn tất bước thanh toán).</summary>
        Pending = 0,
        /// <summary>Đang chờ cổng thanh toán (ví dụ QR PayOS).</summary>
        Processing = 1,
        /// <summary>Đã quyết toán thành công.</summary>
        Paid = 2,
        /// <summary>Thanh toán thất bại.</summary>
        Failed = 3,
        /// <summary>Đã hủy (ví dụ user hủy link QR).</summary>
        Cancelled = 4,
        /// <summary>Hoàn tiền.</summary>
        Refunded = 5
    }

    /// <summary>
    /// Thời điểm quyết toán tiền so với giao hàng / xuất kho (khác với <see cref="PaymentMethod"/> — cách trả tiền).
    /// </summary>
    public enum PaymentTiming
    {
        /// <summary>Trả trước: phải Paid (hoặc đủ điều kiện thanh toán trước) trước khi hoàn tất xuất/pick theo rule.</summary>
        PayBefore = 0,

        /// <summary>Trả sau (thu khi giao / sau khi xuất tùy flow): cho phép tiền mặt <see cref="PaymentMethod.Cash"/> ở Pending khi tạo phiếu xuất; có thể ghi nhận Paid khi Delivered.</summary>
        PayAfter = 1
    }

    /// <summary>Cách thanh toán (tiền mặt, ví, cổng…). Giá trị int lưu DB; 0 = Cash (tương thích bản ghi cũ từng dùng tên COD).</summary>
    public enum PaymentMethod
    {
        /// <summary>Tiền mặt tại quầy hoặc thu khi giao — tạo bản ghi Pending, xác nhận → Paid.</summary>
        Cash = 0,
        VNPay = 1,
        Momo = 2,
        /// <summary>QR / chuyển khoản (PayOS).</summary>
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
