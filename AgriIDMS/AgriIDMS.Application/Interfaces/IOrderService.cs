using AgriIDMS.Application.DTOs.Order;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AgriIDMS.Application.Interfaces
{
    public interface IOrderService
    {
        Task<IList<OrderListItemDto>> GetMyOrdersAsync(string userId, GetOrdersQuery query);

        Task<OrderDetailDto> GetMyOrderByIdAsync(int orderId, string userId);

        Task<CreateOrderFromCartResponse> CreateOrderFromCartAsync(string userId);


        Task<CreateOrderFromCartResponse> CreateOrderFromCartByVariantIdsAsync(string userId, IList<CreateOrderFromCartByVariantIdsRequest> requestItems);

        Task AllocateInventoryAsync(int orderId, string userId);
    }
}
