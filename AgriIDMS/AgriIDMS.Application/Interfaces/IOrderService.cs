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

        /// <summary>Kiểm tra tồn, giữ hàng (reserve box) và chuyển đơn sang Confirmed nếu đủ hàng.</summary>
        Task ConfirmOrderAsync(int orderId, string userId);
    }
}
