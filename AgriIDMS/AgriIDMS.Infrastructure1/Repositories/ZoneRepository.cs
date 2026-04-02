using AgriIDMS.Domain.Entities;
using AgriIDMS.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AgriIDMS.Infrastructure.Repositories
{
    public class ZoneRepository : IZoneRepository
    {
        private readonly AppDbContext _context;

        public ZoneRepository(AppDbContext context)
        {
            _context = context;
        }

        public Task<List<Zone>> GetByWarehouseAsync(int warehouseId)
        {
            return _context.Zones
                .AsNoTracking()
                .Where(z => z.WarehouseId == warehouseId)
                .OrderBy(z => z.Name)
                .ToListAsync();
        }

        public Task<Zone?> GetByIdAsync(int id)
        {
            return _context.Zones
                .FirstOrDefaultAsync(z => z.Id == id);
        }

        public Task<bool> ExistsByNameAsync(int warehouseId, string name)
        {
            return _context.Zones
                .AnyAsync(z => z.WarehouseId == warehouseId && z.Name == name);
        }

        public async Task AddAsync(Zone zone)
        {
            await _context.Zones.AddAsync(zone);
        }

        public Task UpdateAsync(Zone zone)
        {
            _context.Zones.Update(zone);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Zone zone)
        {
            _context.Zones.Remove(zone);
            return Task.CompletedTask;
        }
    }
}

