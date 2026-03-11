using AgriIDMS.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AgriIDMS.Domain.Interfaces
{
    public interface ICartRepository
    {
        Task<Cart?> GetByUserIdWithItemsAsync(string userId);

        Task AddAsync(Cart cart);

        Task UpdateAsync(Cart cart);

        Task ClearCartAsync(Cart cart);
    }
}

