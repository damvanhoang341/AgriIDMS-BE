using AgriIDMS.Application.DTOs.Order;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AgriIDMS.Application.Interfaces
{
    public interface IOrderService
    {
        Task<IList<OrderListItemDto>> GetMyOrdersAsync(string userId, GetOrdersQuery query);

        Task<OrderDetailDto> GetMyOrderByIdAsync(int orderId, string userId);

        /// <summary>Hủy đơn trước khi vào bước shipping (tức là chỉ cho phép khi đơn chưa được thanh toán).</summary>
        Task CancelOrderAsync(int orderId, string userId);

        Task<CreateOrderFromCartResponse> CreateOrderFromCartAsync(string userId);


        Task<CreateOrderFromCartResponse> CreateOrderFromCartByVariantIdsAsync(string userId, IList<CreateOrderFromCartByVariantIdsRequest> requestItems);

        /// <summary>Sale xác nhận đơn hợp lệ → chuyển sang chờ giữ hàng (AwaitingAllocation).</summary>
        Task SaleConfirmOrderAsync(int orderId, string confirmedByUserId);

        /// <summary>Giữ hàng (allocate): chỉ khi đơn đã AwaitingAllocation (hoặc AwaitingPayment — đơn cũ). Nên bật phân quyền staff khi <paramref name="skipCustomerOwnershipCheck"/> = true.</summary>
        Task ConfirmOrderAsync(int orderId, string operatorUserId, bool skipCustomerOwnershipCheck = false);
    }
}
