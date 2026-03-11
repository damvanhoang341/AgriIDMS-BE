using System.Threading.Tasks;

namespace AgriIDMS.Application.Interfaces
{
    public interface IOrderService
    {
        Task<int> CreateOrderFromCartAsync(string userId);

        Task AllocateInventoryAsync(int orderId, string userId);
    }
}
