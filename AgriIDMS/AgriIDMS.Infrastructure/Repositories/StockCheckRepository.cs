using AgriIDMS.Domain.Entities;
using AgriIDMS.Domain.Enums;
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
                    .ThenInclude(d => d.CountedUser)
                .Include(s => s.Details)
                    .ThenInclude(d => d.Box)
                        .ThenInclude(b => b.Lot)
                .Include(s => s.Details)
                    .ThenInclude(d => d.Box)
                        .ThenInclude(b => b.Slot)
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

        public async Task<List<int>> GetBoxIdsForCycleAsync(
            int warehouseId,
            int? zoneId,
            int? rackId,
            int? slotId)
        {
            var slotQuery = _context.Slots
                .Where(s => s.Rack.Zone.WarehouseId == warehouseId);

            if (zoneId.HasValue && zoneId.Value > 0)
                slotQuery = slotQuery.Where(s => s.Rack.ZoneId == zoneId.Value);

            if (rackId.HasValue && rackId.Value > 0)
                slotQuery = slotQuery.Where(s => s.RackId == rackId.Value);

            if (slotId.HasValue && slotId.Value > 0)
                slotQuery = slotQuery.Where(s => s.Id == slotId.Value);

            return await slotQuery
                .SelectMany(s => s.Boxes.Select(b => b.Id))
                .Distinct()
                .ToListAsync();
        }

        public async Task<List<StockCheck>> GetStockChecksWithDetailsAsync(
            int? warehouseId,
            IEnumerable<StockCheckStatus> statuses)
        {
            var statusList = statuses?.ToList() ?? new List<StockCheckStatus>();

            var query = _context.StockChecks
                .AsQueryable()
                .Include(s => s.Warehouse)
                .Include(s => s.Details)
                .Where(s => statusList.Contains(s.Status));

            if (warehouseId.HasValue)
                query = query.Where(s => s.WarehouseId == warehouseId.Value);

            return await query
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();
        }
    }
}
