using AgriIDMS.Domain.Entities;
using AgriIDMS.Domain.Enums;
using AgriIDMS.Domain.Interfaces;
using AgriIDMS.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;
using System;
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

        public async Task<List<Box>> GetByIdsWithLotAndReceiptAsync(IEnumerable<int> ids)
        {
            var idList = ids.Distinct().ToList();
            if (idList.Count == 0) return new List<Box>();
            return await _context.Boxes
                .Include(b => b.Lot)
                    .ThenInclude(l => l.GoodsReceiptDetail)
                        .ThenInclude(d => d!.GoodsReceipt)
                .Include(b => b.Slot)
                .Where(b => idList.Contains(b.Id))
                .ToListAsync();
        }

        public Task UpdateAsync(Box box)
        {
            _context.Boxes.Update(box);
            return Task.CompletedTask;
        }

        public async Task<Box?> GetByQrCodeAsync(string qrCode)
        {
            return await _context.Boxes
                .Include(b => b.Lot)
                    .ThenInclude(l => l.GoodsReceiptDetail)
                        .ThenInclude(d => d.GoodsReceipt)
                .Include(b => b.Slot)
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.QRCode == qrCode);
        }

        public async Task<List<Box>> GetAvailableBoxesForVariantAsync(int productVariantId)
        {
            return await _context.Boxes
                .Include(b => b.Lot)
                    .ThenInclude(l => l.GoodsReceiptDetail)
                .Include(b => b.Slot)
                    .ThenInclude(s => s!.Rack)
                        .ThenInclude(r => r.Zone)
                            .ThenInclude(z => z.Warehouse)
                .Where(b =>
                    b.Lot.GoodsReceiptDetail.ProductVariantId == productVariantId &&
                    b.Status == BoxStatus.Stored &&
                    b.Lot.Status == LotStatus.Active &&
                    b.Lot.ExpiryDate > System.DateTime.UtcNow)
                .OrderBy(b => b.Lot.ExpiryDate)
                .ToListAsync();
        }

        public async Task<int> GetAvailableBoxCountByVariantIdAsync(int productVariantId)
        {
            return await _context.Boxes
                .Include(b => b.Lot)
                    .ThenInclude(l => l.GoodsReceiptDetail)
                .Where(b =>
                    b.Lot.GoodsReceiptDetail.ProductVariantId == productVariantId &&
                    b.Status == BoxStatus.Stored &&
                    b.Lot.Status == LotStatus.Active &&
                    b.Lot.ExpiryDate > DateTime.UtcNow)
                .CountAsync();
        }

        public async Task<int> GetAvailableBoxCountByVariantAndTypeAsync(int productVariantId, bool isPartial, decimal weight)
        {
            var now = DateTime.UtcNow;

            // Query tối ưu: không Include toàn bộ entity, chỉ dùng navigation để filter ExpiryDate.
            var query = _context.Boxes
                .Where(b =>
                    b.Lot.GoodsReceiptDetail.ProductVariantId == productVariantId &&
                    b.Status == BoxStatus.Stored &&
                    b.Lot.Status == LotStatus.Active &&
                    b.IsPartial == isPartial &&
                    b.Weight == weight);

            // 1) Kiểm tra nhanh xem có box hết hạn / ExpiryDate <= now không
            bool hasExpired = await query.AnyAsync(b => b.Lot.ExpiryDate <= now);
            if (hasExpired)
            {
                throw new InvalidBusinessRuleException("Có box thuộc loại này đã hết hạn hoặc ExpiryDate không hợp lệ.");
            }

            // 2) Đếm số box còn hạn
            return await query.CountAsync(b => b.Lot.ExpiryDate > now);
        }

        public async Task<List<BoxTypeSummary>> GetAvailableBoxTypeSummaryByVariantIdAsync(int productVariantId)
        {
            return await _context.Boxes
                .Include(b => b.Lot)
                    .ThenInclude(l => l.GoodsReceiptDetail)
                .Where(b =>
                    b.Lot.GoodsReceiptDetail.ProductVariantId == productVariantId &&
                    b.Status == BoxStatus.Stored &&
                    b.Lot.Status == LotStatus.Active &&
                    b.Lot.ExpiryDate > DateTime.UtcNow)
                .GroupBy(b => new { b.IsPartial, b.Weight })
                .Select(g => new BoxTypeSummary
                {
                    IsPartial = g.Key.IsPartial,
                    Weight = g.Key.Weight,
                    AvailableCount = g.Count()
                })
                .ToListAsync();
        }
    }
}
