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

    /// <summary>POS TakeAway: đơn mới chỉ <see cref="PayBeforePick"/>. <see cref="PickBeforePay"/> = dữ liệu legacy.</summary>
    public enum PosCheckoutTiming
    {
        /// <summary>Legacy (trước đây gắn PayAfter).</summary>
        PickBeforePay = 0,

        /// <summary>Trả trước: Paid mới tạo phiếu xuất; Delivered khi duyệt xuất.</summary>
        PayBeforePick = 1
    }

    public enum OrderStatus
    {
        /// <summary>Giá trị legacy (DB cũ); luồng hiện tại không gán trạng thái này.</summary>
        AwaitingPayment = 0,
        Confirmed = 2,
        /// <summary>Giá trị legacy; luồng hiện tại không gán.</summary>
        InventoryFailed = 3,

        /// <summary>Đơn Delivery: đã duyệt xuất kho, chờ lấy hàng / vận chuyển (trước đây tên <c>Shipping</c>).</summary>
        ApprovedExport = 4,

        Completed = 5,
        Cancelled = 6,
        Refunded = 7,

        /// <summary>Vừa tạo từ giỏ — chờ sale xác nhận trước khi được giữ hàng.</summary>
        PendingSaleConfirmation = 8,

        /// <summary>Chờ giữ tồn FEFO (chủ yếu POS Delivery); đơn online từ giỏ thường Confirmed ngay sau sale-confirm.</summary>
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

    /// <summary>Tiến trình giao bên thứ ba (<see cref="FulfillmentType.Delivery"/>). <see cref="FulfillmentType.TakeAway"/> luôn <see cref="None"/> (khách tự mang).</summary>
    public enum ShippingStatus
    {
        None = 0,
        ShippingPendingPickup = 1,
        ShippingInProgress = 2,
        DeliveredShip = 3,
        ShippingFailed = 4
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

        /// <summary>Trả sau: đơn online có thể xuất kho khi chưa thanh toán; tiền mặt Pending vẫn cho phép xuất; quyết toán khi giao (Delivered) hoặc xác nhận thu.</summary>
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
