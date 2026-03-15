using AgriIDMS.Application.DTOs.Order;
using System.Threading.Tasks;

namespace AgriIDMS.Application.Interfaces
{
    public interface IOrderService
    {
        Task<CreateOrderFromCartResponse> CreateOrderFromCartAsync(string userId);

        Task AllocateInventoryAsync(int orderId, string userId);
    }
}
