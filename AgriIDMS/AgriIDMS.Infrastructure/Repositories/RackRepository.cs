using AgriIDMS.Domain.Entities;
using AgriIDMS.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AgriIDMS.Infrastructure.Repositories
{
    public class RackRepository : IRackRepository
    {
        private readonly AppDbContext _context;

        public RackRepository(AppDbContext context)
        {
            _context = context;
        }

        public Task<List<Rack>> GetByZoneAsync(int zoneId)
        {
            return _context.Racks
                .AsNoTracking()
                .Where(r => r.ZoneId == zoneId)
                .OrderBy(r => r.Name)
                .ToListAsync();
        }

        public Task<Rack?> GetByIdAsync(int id)
        {
            return _context.Racks
                .FirstOrDefaultAsync(r => r.Id == id);
        }

        public Task<bool> ExistsByNameAsync(int zoneId, string name)
        {
            return _context.Racks
                .AnyAsync(r => r.ZoneId == zoneId && r.Name == name);
        }

        public async Task AddAsync(Rack rack)
        {
            await _context.Racks.AddAsync(rack);
        }

        public Task UpdateAsync(Rack rack)
        {
            _context.Racks.Update(rack);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Rack rack)
        {
            _context.Racks.Remove(rack);
            return Task.CompletedTask;
        }
    }
}

