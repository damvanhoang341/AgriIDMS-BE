using AgriIDMS.Domain.Entities;
using AgriIDMS.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AgriIDMS.Infrastructure.Repositories
{
    public class PurchaseRequestRepository : IPurchaseRequestRepository
    {
        private readonly AppDbContext _context;

        public PurchaseRequestRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(PurchaseRequest entity)
        {
            await _context.PurchaseRequests.AddAsync(entity);
        }

        public async Task<IEnumerable<PurchaseRequest>> GetAllAsync()
        {
            return await _context.PurchaseRequests
                .Include(x => x.Details)
                    .ThenInclude(d => d.Product)
                .OrderByDescending(x => x.Id)
                .ToListAsync();
        }

        public async Task<PurchaseRequest?> GetByIdAsync(int id)
        {
            return await _context.PurchaseRequests
                .Include(x => x.Details)
                    .ThenInclude(d => d.Product)
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<string> GenerateRequestCodeAsync()
        {
            var count = await _context.PurchaseRequests.CountAsync() + 1;
            return $"PR-{DateTime.UtcNow:yyyyMMdd}-{count:D4}";
        }
    }
}
