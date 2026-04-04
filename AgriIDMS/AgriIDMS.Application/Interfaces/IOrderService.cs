using AgriIDMS.Application.DTOs.Order;
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
        Task<IList<OrderListItemDto>> GetPendingCustomerDecisionOrdersAsync(GetPendingAllocationOrdersQuery query);
        Task<IList<BackorderWaitingListItemDto>> GetBackorderWaitingOrdersAsync(GetPendingAllocationOrdersQuery query);
        Task<BackorderWaitingDetailDto> GetBackorderWaitingOrderDetailAsync(int orderId);
        Task<IList<OrderListItemDto>> GetConfirmedAllocationOrdersAsync(GetPendingAllocationOrdersQuery query);
        Task<AllocationProposalOverviewDto> GetAllocationProposalsAsync(int orderId);
        Task<AllocationHistoryDto> GetAllocationHistoryAsync(int orderId);
        Task ConfirmDeliveredAsync(int orderId, string operatorUserId);
        Task ConfirmFailedDeliveryAsync(int orderId, string operatorUserId);
        Task ConfirmReturnedAsync(int orderId, string operatorUserId);
        /// <summary>Xác nhận đã thu tiền mặt cho đơn (theo payment Cash Pending mới nhất).</summary>
        Task ConfirmCashPaidForOrderAsync(int orderId, string operatorUserId);
        Task<bool> CheckCanCompleteAsync(int orderId);
        Task<int> AutoCompleteOrdersAsync();

        Task<OrderDetailDto> GetMyOrderByIdAsync(int orderId, string userId);

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

        /// <summary>Sale xác nhận đơn hợp lệ → chuyển sang chờ giữ hàng (AwaitingAllocation).</summary>
        Task<SaleConfirmOrderResponseDto> SaleConfirmOrderAsync(int orderId, string confirmedByUserId);

        /// <summary>Giữ hàng (allocate): chỉ khi đơn đã AwaitingAllocation (hoặc AwaitingPayment — đơn cũ). Nên bật phân quyền staff khi <paramref name="skipCustomerOwnershipCheck"/> = true.</summary>
        Task ConfirmOrderAsync(int orderId, string operatorUserId, bool skipCustomerOwnershipCheck = false);
        Task<AllocationProposalResultDto> AutoProposeAllocationAsync(int orderId, string operatorUserId, bool skipCustomerOwnershipCheck = false);
        Task<AllocationProposalResultDto> ReProposeAllocationAsync(int orderId, string operatorUserId, bool skipCustomerOwnershipCheck = false);
        Task<AllocationProposalResultDto> RejectAllocationProposalAsync(int orderId, string operatorUserId, bool skipCustomerOwnershipCheck = false);
        Task<ConfirmAllocationResultDto> ConfirmAllocationAsync(int orderId, string operatorUserId, bool skipCustomerOwnershipCheck = false);

        /// <summary>Khách chọn chờ backorder cho phần còn thiếu.</summary>
        Task WaitBackorderAsync(int orderId, string userId);
        /// <summary>Sales staff chọn chờ backorder thay khách cho phần còn thiếu.</summary>
        Task WaitBackorderAsStaffAsync(int orderId, string operatorUserId);

        /// <summary>Khách chấp nhận bỏ phần còn thiếu: chỉ ship phần đã allocate/giữ được.</summary>
        Task CancelShortageAsync(int orderId, string userId);
        /// <summary>Sales staff chọn hủy phần thiếu thay khách.</summary>
        Task CancelShortageAsStaffAsync(int orderId, string operatorUserId);

        /// <summary>
        /// Staff allocate nốt phần còn thiếu cho backorder.
        /// Nếu vượt quá thời gian chờ thì hệ thống xử lý theo <paramref name="expiredAction"/>.
        /// </summary>
        Task BackorderAllocateAsync(int orderId, string operatorUserId, BackorderExpiredAction expiredAction);

        /// <summary>Danh sách đơn backorder đã quá hạn để sale/staff liên hệ khách và xử lý decision.</summary>
        Task<IList<OverdueBackorderItemDto>> GetOverdueBackordersAsync();
    }
}
