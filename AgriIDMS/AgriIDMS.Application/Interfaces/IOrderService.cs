using AgriIDMS.Application.DTOs.Order;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AgriIDMS.Application.Interfaces
{
    public interface IOrderService
    {
        Task<IList<OrderListItemDto>> GetMyOrdersAsync(string userId, GetOrdersQuery query);
        Task<IList<OrderListItemDto>> GetPendingSaleConfirmOrdersAsync(GetPendingSaleConfirmOrdersQuery query);
        Task<IList<OrderListItemDto>> GetPendingAllocationOrdersAsync(GetPendingAllocationOrdersQuery query);

        Task<OrderDetailDto> GetMyOrderByIdAsync(int orderId, string userId);

        /// <summary>Hủy đơn trước khi vào bước shipping (tức là chỉ cho phép khi đơn chưa được thanh toán).</summary>
        Task CancelOrderAsync(int orderId, string userId);

        Task<CreateOrderFromCartResponse> CreateOrderFromCartAsync(string userId);


        Task<CreateOrderFromCartResponse> CreateOrderFromCartByVariantIdsAsync(string userId, IList<CreateOrderFromCartByVariantIdsRequest> requestItems);
        Task<CreateOrderFromCartResponse> CreatePosOrderAsync(string operatorUserId, CreatePosOrderRequest request);

        /// <summary>Sale xác nhận đơn hợp lệ → chuyển sang chờ giữ hàng (AwaitingAllocation).</summary>
        Task SaleConfirmOrderAsync(int orderId, string confirmedByUserId);

        /// <summary>Giữ hàng (allocate): chỉ khi đơn đã AwaitingAllocation (hoặc AwaitingPayment — đơn cũ). Nên bật phân quyền staff khi <paramref name="skipCustomerOwnershipCheck"/> = true.</summary>
        Task ConfirmOrderAsync(int orderId, string operatorUserId, bool skipCustomerOwnershipCheck = false);

        /// <summary>Khách chọn chờ backorder cho phần còn thiếu.</summary>
        Task WaitBackorderAsync(int orderId, string userId);

        /// <summary>Khách chấp nhận bỏ phần còn thiếu: chỉ ship phần đã allocate/giữ được.</summary>
        Task CancelShortageAsync(int orderId, string userId);

        /// <summary>
        /// Staff allocate nốt phần còn thiếu cho backorder.
        /// Nếu vượt quá thời gian chờ thì hệ thống xử lý theo <paramref name="expiredAction"/>.
        /// </summary>
        Task BackorderAllocateAsync(int orderId, string operatorUserId, BackorderExpiredAction expiredAction);

        /// <summary>Danh sách đơn backorder đã quá hạn để sale/staff liên hệ khách và xử lý decision.</summary>
        Task<IList<OverdueBackorderItemDto>> GetOverdueBackordersAsync();
    }
}
