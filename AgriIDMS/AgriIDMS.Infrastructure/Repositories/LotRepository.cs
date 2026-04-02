using AgriIDMS.Domain.Entities;
using AgriIDMS.Domain.Enums;
using AgriIDMS.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgriIDMS.Infrastructure.Repositories
{
    public class LotRepository : ILotRepository
    {
        private readonly AppDbContext _context;

        public LotRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task AddRangeAsync(IEnumerable<Lot> lots)
        {
            await _context.Lots.AddRangeAsync(lots);
        }

        public async Task<Lot?> GetByIdAsync(int id)
        {
            return await _context.Lots.FindAsync(id);
        }

        public async Task<Lot?> GetByIdWithContextAndBoxesAsync(int id)
        {
            return await _context.Lots
                .Include(l => l.GoodsReceiptDetail)
                    .ThenInclude(d => d.ProductVariant)
                        .ThenInclude(v => v.Product)
                .Include(l => l.GoodsReceiptDetail)
                    .ThenInclude(d => d.GoodsReceipt)
                        .ThenInclude(r => r.Warehouse)
                .Include(l => l.Boxes)
                    .ThenInclude(b => b.Slot)
                .FirstOrDefaultAsync(l => l.Id == id);
        }

        public async Task<Lot?> GetByIdWithDetailAndReceiptAsync(int id)
        {
            return await _context.Lots
                .Include(l => l.GoodsReceiptDetail)
                    .ThenInclude(d => d.ProductVariant)
                        .ThenInclude(v => v.Product)
                .Include(l => l.GoodsReceiptDetail)
                    .ThenInclude(d => d!.GoodsReceipt)
                        .ThenInclude(gr => gr.Warehouse)
                .Include(l => l.Boxes)
                    .ThenInclude(b => b.Slot)
                .FirstOrDefaultAsync(l => l.Id == id);
        }

        public async Task<List<Lot>> GetByGoodsReceiptIdAsync(int goodsReceiptId)
        {
            return await _context.Lots
                .Where(l => l.GoodsReceiptDetail.GoodsReceiptId == goodsReceiptId)
                .ToListAsync();
        }

        public async Task<Lot?> GetByLotCodeAsync(string lotCode)
        {
            return await _context.Lots
                .Include(l => l.GoodsReceiptDetail)
                    .ThenInclude(d => d.GoodsReceipt)
                .Include(l => l.GoodsReceiptDetail)
                    .ThenInclude(d => d.ProductVariant)
                        .ThenInclude(pv => pv.Product)
                .FirstOrDefaultAsync(l => l.LotCode == lotCode);

        }

        public async Task<List<Lot>> GetAllWithContextAsync()
        {
            return await _context.Lots
                .Include(l => l.GoodsReceiptDetail)
                    .ThenInclude(d => d.GoodsReceipt)
                        .ThenInclude(r => r.Warehouse)
                .Include(l => l.GoodsReceiptDetail)
                    .ThenInclude(d => d.ProductVariant)
                        .ThenInclude(v => v.Product)
                .OrderByDescending(l => l.ReceivedDate)
                .ToListAsync();
        }

        public async Task<IEnumerable<Lot>> GetAllExpiryDateAsync()
        {
            var now = DateTime.UtcNow;

            return await _context.Lots
                .Include(l => l.GoodsReceiptDetail)
                    .ThenInclude(d => d.ProductVariant)
                        .ThenInclude(v => v.Product)
                .Where(l => l.ExpiryDate <= now.AddDays(3)
                         && l.ExpiryDate >= now
                         && l.RemainingQuantity > 0
                         && l.Status == LotStatus.Active)
                .OrderBy(l => l.ExpiryDate) // 
                .ToListAsync();
        }

        public async Task<List<Lot>> GetNearExpiryLotsAsync(int days, int? warehouseId = null)
        {
            var now = DateTime.UtcNow;
            var deadline = now.AddDays(days);

            var query = _context.Lots
                .Include(l => l.GoodsReceiptDetail)
                    .ThenInclude(d => d.ProductVariant)
                        .ThenInclude(v => v.Product)
                .Include(l => l.GoodsReceiptDetail)
                    .ThenInclude(d => d.GoodsReceipt)
                        .ThenInclude(gr => gr.Warehouse)
                .Include(l => l.Boxes)
                    .ThenInclude(b => b.Slot)
                .Where(l =>
                    l.ExpiryDate <= deadline &&
                    l.Status == LotStatus.Active &&
                    // Use real box stock (Stored/Reserved, weight > 0) instead of Lot.RemainingQuantity
                    // to avoid stale values after stock-check.
                    l.Boxes.Any(b =>
                        (b.Status == BoxStatus.Stored || b.Status == BoxStatus.Reserved) &&
                        b.Weight > 0m));

            if (warehouseId.HasValue && warehouseId.Value > 0)
            {
                query = query.Where(
                    l => l.GoodsReceiptDetail.GoodsReceipt.WarehouseId == warehouseId.Value);
            }

            return await query
                .OrderBy(l => l.ExpiryDate)
                .ToListAsync();
        }

    }
}
