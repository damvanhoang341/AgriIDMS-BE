using AgriIDMS.Domain.Entities;
using AgriIDMS.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AgriIDMS.Infrastructure.Repositories
{
    public class StockCheckRepository : IStockCheckRepository
    {
        private readonly AppDbContext _context;

        public StockCheckRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(StockCheck stockCheck)
        {
            await _context.StockChecks.AddAsync(stockCheck);
        }

        public Task<StockCheck?> GetByIdAsync(int id)
        {
            return _context.StockChecks
                .Include(s => s.Warehouse)
                .FirstOrDefaultAsync(s => s.Id == id);
        }

        public Task<StockCheck?> GetByIdWithDetailsAndBoxesAsync(int id)
        {
            return _context.StockChecks
                .Include(s => s.Warehouse)
                .Include(s => s.Details)
                    .ThenInclude(d => d.Box)
                .FirstOrDefaultAsync(s => s.Id == id);
        }

        public Task UpdateAsync(StockCheck stockCheck)
        {
            _context.StockChecks.Update(stockCheck);
            return Task.CompletedTask;
        }

        public async Task<List<int>> GetBoxIdsInWarehouseAsync(int warehouseId)
        {
            return await _context.Slots
                .Where(s => s.Rack.Zone.WarehouseId == warehouseId)
                .SelectMany(s => s.Boxes.Select(b => b.Id))
                .ToListAsync();
        }
    }
}
