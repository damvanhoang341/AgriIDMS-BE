using AgriIDMS.Domain.Entities;
using AgriIDMS.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AgriIDMS.Infrastructure.Repositories
{
    public class SlotRepository : ISlotRepository
    {
        private readonly AppDbContext _context;

        public SlotRepository(AppDbContext context)
        {
            _context = context;
        }

        public Task<List<Slot>> GetByRackAsync(int rackId)
        {
            return _context.Slots
                .AsNoTracking()
                .Where(s => s.RackId == rackId)
                .OrderBy(s => s.Code)
                .ToListAsync();
        }

        public Task<Slot?> GetByIdAsync(int id)
        {
            return _context.Slots
                .FirstOrDefaultAsync(s => s.Id == id);
        }

        public async Task AddAsync(Slot slot)
        {
            await _context.Slots.AddAsync(slot);
        }

        public Task UpdateAsync(Slot slot)
        {
            _context.Slots.Update(slot);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(Slot slot)
        {
            _context.Slots.Remove(slot);
            return Task.CompletedTask;
        }
    }
}

