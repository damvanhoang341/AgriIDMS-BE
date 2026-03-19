using AgriIDMS.Application.DTOs.Order;
using System.Threading.Tasks;

namespace AgriIDMS.Application.Interfaces
{
    public interface IOrderService
    {
        Task<CreateOrderFromCartResponse> CreateOrderFromCartAsync(string userId);


        Task<CreateOrderFromCartResponse> CreateOrderFromCartByVariantIdsAsync(string userId, System.Collections.Generic.IList<int> productVariantIds);

        Task AllocateInventoryAsync(int orderId, string userId);
    }
}
