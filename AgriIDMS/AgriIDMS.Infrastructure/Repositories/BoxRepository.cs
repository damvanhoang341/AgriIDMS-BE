using AgriIDMS.Domain.Entities;
using AgriIDMS.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AgriIDMS.Infrastructure.Repositories
{
    public class BoxRepository : IBoxRepository
    {
        private readonly AppDbContext _context;
        public BoxRepository(AppDbContext context)
        {
            _context = context;
        }
        public async Task CreateAsync(Box box)
        {
            await _context.Boxes.AddAsync(box);
        }

        public async Task<Box?> GetByIdAsync(int id)
        {
            return await _context.Boxes.FindAsync(id);
        }

        public async Task<Box?> GetByIdWithLotAndReceiptAsync(int id)
        {
            return await _context.Boxes
                .Include(b => b.Lot)
                    .ThenInclude(l => l.GoodsReceiptDetail)
                        .ThenInclude(d => d!.GoodsReceipt)
                .Include(b => b.Slot)
                .FirstOrDefaultAsync(b => b.Id == id);
        }

        public async Task<Dictionary<int, Box>> GetByIdsAsync(IEnumerable<int> ids)
        {
            var idList = ids.Distinct().ToList();
            if (idList.Count == 0) return new Dictionary<int, Box>();
            var boxes = await _context.Boxes.Where(b => idList.Contains(b.Id)).ToListAsync();
            return boxes.ToDictionary(b => b.Id);
        }

        public Task UpdateAsync(Box box)
        {
            _context.Boxes.Update(box);
            return Task.CompletedTask;
        }
    }
}
