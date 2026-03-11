using AgriIDMS.Domain.Entities;
using AgriIDMS.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

namespace AgriIDMS.Infrastructure.Repositories
{
    public class CartRepository : ICartRepository
    {
        private readonly AppDbContext _context;

        public CartRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Cart?> GetByUserIdWithItemsAsync(string userId)
        {
            return await _context.Carts
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.UserId == userId);
        }

        public async Task AddAsync(Cart cart)
        {
            await _context.Carts.AddAsync(cart);
        }

        public Task UpdateAsync(Cart cart)
        {
            _context.Carts.Update(cart);
            return Task.CompletedTask;
        }

        public Task ClearCartAsync(Cart cart)
        {
            if (cart.Items != null && cart.Items.Any())
            {
                _context.CartItems.RemoveRange(cart.Items);
            }

            cart.Items.Clear();
            cart.UpdatedAt = System.DateTime.UtcNow;

            _context.Carts.Update(cart);
            return Task.CompletedTask;
        }
    }
}

