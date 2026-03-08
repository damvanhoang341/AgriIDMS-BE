using AgriIDMS.Domain.Entities;
using AgriIDMS.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AgriIDMS.Infrastructure.Repositories
{
    public class StockCheckDetailRepository : IStockCheckDetailRepository
    {
        private readonly AppDbContext _context;

        public StockCheckDetailRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task AddRangeAsync(IEnumerable<StockCheckDetail> details)
        {
            await _context.StockCheckDetails.AddRangeAsync(details);
        }

        public Task<StockCheckDetail?> GetByIdAsync(int id)
        {
            return _context.StockCheckDetails
                .Include(d => d.StockCheck)
                .Include(d => d.Box)
                .FirstOrDefaultAsync(d => d.Id == id);
        }

        public Task UpdateAsync(StockCheckDetail detail)
        {
            _context.StockCheckDetails.Update(detail);
            return Task.CompletedTask;
        }
    }
}
