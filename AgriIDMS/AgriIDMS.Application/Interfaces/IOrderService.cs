using AgriIDMS.Application.DTOs.Order;
using AgriIDMS.Domain.Enums;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AgriIDMS.Application.Interfaces
{
    public interface IOrderService
    {
        Task<IList<OrderListItemDto>> GetMyOrdersAsync(string userId, GetOrdersQuery query);
        Task<IList<OrderListItemDto>> GetPendingSaleConfirmOrdersAsync(GetPendingSaleConfirmOrdersQuery query);
        Task<IList<PaidPendingExportOrderListItemDto>> GetPaidPendingExportOrdersAsync(GetPaidPendingExportOrdersQuery query);
        Task<IList<OrderListItemDto>> GetPendingAllocationOrdersAsync(GetPendingAllocationOrdersQuery query);
        Task<IList<OrderListItemDto>> GetPendingWarehouseConfirmOrdersAsync(GetPendingAllocationOrdersQuery query);
        Task<IList<OrderListItemDto>> GetConfirmedAllocationOrdersAsync(GetPendingAllocationOrdersQuery query);
        Task<AllocationProposalOverviewDto> GetAllocationProposalsAsync(int orderId);
        Task<AllocationHistoryDto> GetAllocationHistoryAsync(int orderId);
        Task ConfirmDeliveredAsync(int orderId, string operatorUserId);
        Task ConfirmFailedDeliveryAsync(int orderId, string operatorUserId);
        Task ConfirmReturnedAsync(int orderId, string operatorUserId);

        /// <summary>
        /// Sale/staff cập nhật tiến trình vận chuyển (Delivery, ApprovedExport): ví dụ ShippingPendingPickup → ShippingInProgress.
        /// </summary>
        Task<OrderDetailDto> UpdateOrderShippingStatusAsStaffAsync(int orderId, ShippingStatus newShippingStatus, string operatorUserId);
        /// <summary>Xác nhận đã thu tiền mặt cho đơn (theo payment Cash Pending mới nhất).</summary>
        Task ConfirmCashPaidForOrderAsync(int orderId, string operatorUserId);
        Task<bool> CheckCanCompleteAsync(int orderId);
        Task<int> AutoCompleteOrdersAsync();

        Task<OrderDetailDto> GetMyOrderByIdAsync(int orderId, string userId);

        /// <summary>Đơn online đã Confirmed: khách chọn trả trước / trả sau (một lần).</summary>
        Task<OrderDetailDto> SetOnlineOrderPaymentTimingAsync(int orderId, string userId, PaymentTiming paymentTiming);

        /// <summary>Hủy đơn trước khi vào bước shipping (tức là chỉ cho phép khi đơn chưa được thanh toán).</summary>
        Task CancelOrderAsync(int orderId, string userId);

        /// <summary>Gợi ý họ tên, SĐT, địa chỉ từ profile để màn checkout điền sẵn.</summary>
        Task<OrderCheckoutDefaultsDto> GetOrderCheckoutDefaultsAsync(string userId);

        Task<CreateOrderFromCartResponse> CreateOrderFromCartAsync(string userId, OrderRecipientCheckoutDto recipient);

        Task<CreateOrderFromCartResponse> CreateOrderFromCartByVariantIdsAsync(
            string userId,
            IList<CreateOrderFromCartByVariantIdsRequest> requestItems,
            OrderRecipientCheckoutDto recipient);
        Task<CreateOrderFromCartResponse> CreatePosOrderAsync(string operatorUserId, CreatePosOrderRequest request);

        /// <summary>Đơn online PendingSaleConfirmation: sale xác nhận → Confirmed, gia hạn giữ thùng (Reserved), chờ khách chọn PayBefore/PayAfter.</summary>
        Task<SaleConfirmOrderResponseDto> SaleConfirmOrderAsync(int orderId, string confirmedByUserId);

        /// <summary>
        /// Nhánh ELSE sau khi khách đặt đơn: không chốt được với khách — đơn online PendingSaleConfirmation → Cancelled + nhả thùng.
        /// </summary>
        Task<SaleRejectOrderResponseDto> SaleRejectOrderAsync(int orderId, string rejectedByUserId);

        /// <summary>Đơn legacy <see cref="OrderStatus.AwaitingAllocation"/>: auto-propose + confirm. POS mới giữ đủ thùng khi tạo đơn.</summary>
        Task ConfirmOrderAsync(int orderId, string operatorUserId, bool skipCustomerOwnershipCheck = false);
        Task<AllocationProposalResultDto> AutoProposeAllocationAsync(int orderId, string operatorUserId, bool skipCustomerOwnershipCheck = false);
        Task<AllocationProposalResultDto> ReProposeAllocationAsync(int orderId, string operatorUserId, bool skipCustomerOwnershipCheck = false);
        Task<AllocationProposalResultDto> RejectAllocationProposalAsync(int orderId, string operatorUserId, bool skipCustomerOwnershipCheck = false);
        Task<ConfirmAllocationResultDto> ConfirmAllocationAsync(int orderId, string operatorUserId, bool skipCustomerOwnershipCheck = false);
    }
}
