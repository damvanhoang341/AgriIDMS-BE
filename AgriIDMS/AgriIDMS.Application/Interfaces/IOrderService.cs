using System.Threading.Tasks;

namespace AgriIDMS.Application.Interfaces
{
    public interface IOrderService
    {
        /// <summary>Tạo đơn hàng từ giỏ của user hiện tại và trả về OrderId.</summary>
        Task<int> CreateOrderFromCartAsync(string userId);

        /// <summary>Kiểm tra và giữ hàng (allocate Box) cho đơn hàng.</summary>
        Task AllocateInventoryAsync(int orderId);
    }
}

