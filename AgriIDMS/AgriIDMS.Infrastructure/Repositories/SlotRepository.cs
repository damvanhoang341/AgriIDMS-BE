using AgriIDMS.Domain.Entities;
using AgriIDMS.Domain.Enums;
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
                .Include(s => s.Boxes)
                    .ThenInclude(b => b.Lot)
                        .ThenInclude(l => l.GoodsReceiptDetail)
                            .ThenInclude(d => d.ProductVariant)
                                .ThenInclude(pv => pv.Product)
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

        public Task<Slot?> GetByIdWithWarehouseAsync(int id)
        {
            return _context.Slots
                .Include(s => s.Rack)
                    .ThenInclude(r => r.Zone)
                        .ThenInclude(z => z.Warehouse)
                .Include(s => s.Boxes)
                    .ThenInclude(b => b.Lot)
                        .ThenInclude(l => l.GoodsReceiptDetail)
                .FirstOrDefaultAsync(s => s.Id == id);
        }

        public Task<Slot?> GetByIdWithContentsAsync(int id)
        {
            return _context.Slots
                .Include(s => s.Rack)
                    .ThenInclude(r => r.Zone)
                        .ThenInclude(z => z.Warehouse)
                .Include(s => s.Boxes)
                    .ThenInclude(b => b.Lot)
                        .ThenInclude(l => l.GoodsReceiptDetail)
                            .ThenInclude(d => d.ProductVariant)
                                .ThenInclude(pv => pv.Product)
                .FirstOrDefaultAsync(s => s.Id == id);
        }

        public Task<Slot?> GetByQrCodeAsync(string qrCode)
        {
            return _context.Slots
                .Include(s => s.Rack)
                    .ThenInclude(r => r.Zone)
                        .ThenInclude(z => z.Warehouse)
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.QrCode == qrCode);
        }

        public Task<Slot?> GetByCodeAsync(string code)
        {
            return _context.Slots
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Code == code);
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

        public async Task RecalculateCurrentCapacityAsync(int slotId)
        {
            var slot = await _context.Slots.FirstOrDefaultAsync(s => s.Id == slotId);
            if (slot == null) return;

            var actual = await _context.Boxes
                .Where(b =>
                    b.SlotId == slotId &&
                    b.Status != BoxStatus.Exported &&
                    b.Weight > 0)
                .SumAsync(b => (decimal?)b.Weight) ?? 0m;

            slot.CurrentCapacity = actual;
        }

        public async Task<int> RecalculateCurrentCapacityByWarehouseAsync(int warehouseId)
        {
            var slots = await _context.Slots
                .Where(s => s.Rack.Zone.WarehouseId == warehouseId)
                .ToListAsync();

            if (slots.Count == 0) return 0;

            var actualBySlot = await _context.Boxes
                .Where(b =>
                    b.SlotId.HasValue &&
                    b.Slot != null &&
                    b.Slot.Rack.Zone.WarehouseId == warehouseId &&
                    b.Status != BoxStatus.Exported &&
                    b.Weight > 0)
                .GroupBy(b => b.SlotId!.Value)
                .Select(g => new { SlotId = g.Key, Total = g.Sum(x => x.Weight) })
                .ToDictionaryAsync(x => x.SlotId, x => x.Total);

            foreach (var slot in slots)
            {
                slot.CurrentCapacity = actualBySlot.TryGetValue(slot.Id, out var total)
                    ? total
                    : 0m;
            }

            return slots.Count;
        }
    }
}

