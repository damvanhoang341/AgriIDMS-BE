using AgriIDMS.Domain.Entities;
using AgriIDMS.Domain.Interfaces;
using System.Threading.Tasks;

namespace AgriIDMS.Infrastructure.Repositories
{
    public class InventoryRequestRepository : IInventoryRequestRepository
    {
        private readonly AppDbContext _context;

        public InventoryRequestRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(InventoryRequest request)
        {
            await _context.InventoryRequest.AddAsync(request);
        }
    }
}
