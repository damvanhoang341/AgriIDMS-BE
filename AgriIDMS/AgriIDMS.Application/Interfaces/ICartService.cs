using AgriIDMS.Application.DTOs.Cart;
using System.Threading.Tasks;

namespace AgriIDMS.Application.Interfaces
{
    public interface ICartService
    {
        Task<CartDto> GetMyCartAsync(string userId);

        Task AddOrUpdateItemAsync(AddCartItemRequest request, string userId);

        Task UpdateItemQuantityAsync(int productVariantId, UpdateCartItemRequest request, string userId);

        Task RemoveItemAsync(int productVariantId, string userId);

        Task ClearCartAsync(string userId);
    }
}
