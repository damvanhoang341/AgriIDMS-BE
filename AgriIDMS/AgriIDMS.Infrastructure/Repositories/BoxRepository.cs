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

        /// <summary>
        /// Box đang được đơn khác giữ: Proposed / Reserved / Picked, hoặc SoftLocked còn hạn.
        /// </summary>
        private static IQueryable<Box> WhereNotBlockedByOrderAllocation(
            IQueryable<Box> query,
            AppDbContext ctx,
            DateTime utcNow)
        {
            return query.Where(b => !ctx.OrderAllocations.Any(a =>
                a.BoxId == b.Id &&
                a.Status != AllocationStatus.Cancelled &&
                (a.Status == AllocationStatus.Reserved ||
                 a.Status == AllocationStatus.Picked ||
                 a.Status == AllocationStatus.Proposed ||
                 (a.Status == AllocationStatus.SoftLocked &&
                  (!a.ExpiredAt.HasValue || a.ExpiredAt > utcNow)))));
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
                            .ThenInclude(gr => gr.Warehouse)
                .Include(b => b.Lot)
                    .ThenInclude(l => l.GoodsReceiptDetail)
                        .ThenInclude(d => d.ProductVariant)
                            .ThenInclude(v => v.Product)
                .Include(b => b.Slot)
                    .ThenInclude(s => s!.Rack)
                        .ThenInclude(r => r.Zone)
                            .ThenInclude(z => z.Warehouse)
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.QRCode == qrCode);
        }

        public async Task<List<Box>> GetUnassignedBoxesByWarehouseIdAsync(int warehouseId)
        {
            var now = DateTime.UtcNow;
            return await _context.Boxes
                .Include(b => b.Lot)
                    .ThenInclude(l => l.GoodsReceiptDetail)
                        .ThenInclude(d => d!.GoodsReceipt)
                .Include(b => b.Lot)
                    .ThenInclude(l => l.GoodsReceiptDetail)
                        .ThenInclude(d => d!.ProductVariant)
                            .ThenInclude(v => v.Product)
                .AsNoTracking()
                .Where(b =>
                    b.SlotId == null &&
                    b.Status == BoxStatus.Stored &&
                    b.Weight > 0 &&
                    b.Lot.Status == LotStatus.Active &&
                    b.Lot.ExpiryDate > now &&
                    b.Lot.GoodsReceiptDetail.GoodsReceipt.WarehouseId == warehouseId)
                .ToListAsync();
        }

        public async Task<List<Box>> GetDamagedBoxesAsync(int? warehouseId = null)
        {
            var query = _context.Boxes
                .Include(b => b.Lot)
                    .ThenInclude(l => l.GoodsReceiptDetail)
                        .ThenInclude(d => d!.GoodsReceipt)
                .Include(b => b.Lot)
                    .ThenInclude(l => l.GoodsReceiptDetail)
                        .ThenInclude(d => d!.ProductVariant)
                            .ThenInclude(v => v.Product)
                .Include(b => b.Slot)
                .AsNoTracking()
                .Where(b => b.Status == BoxStatus.Damaged);

            if (warehouseId.HasValue && warehouseId.Value > 0)
            {
                query = query.Where(
                    b => b.Lot.GoodsReceiptDetail.GoodsReceipt.WarehouseId == warehouseId.Value);
            }

            return await query
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<Box>> GetExpiredBoxesByWarehouseIdAsync(int warehouseId)
        {
            var now = DateTime.UtcNow;
            return await _context.Boxes
                .Include(b => b.Lot)
                    .ThenInclude(l => l.GoodsReceiptDetail)
                        .ThenInclude(d => d!.GoodsReceipt)
                .Include(b => b.Lot)
                    .ThenInclude(l => l.GoodsReceiptDetail)
                        .ThenInclude(d => d!.ProductVariant)
                            .ThenInclude(v => v.Product)
                .Include(b => b.Slot)
                .Where(b =>
                    b.Lot.GoodsReceiptDetail.GoodsReceipt.WarehouseId == warehouseId &&
                    b.Lot.ExpiryDate <= now &&
                    b.Status != BoxStatus.Exported &&
                    b.Status != BoxStatus.Disposed &&
                    b.Weight > 0)
                .OrderBy(b => b.Lot.ExpiryDate)
                .ThenBy(b => b.BoxCode)
                .ToListAsync();
        }

        public async Task<List<Box>> GetByGoodsReceiptIdAsync(int goodsReceiptId)
        {
            return await _context.Boxes
                .Include(b => b.Lot)
                    .ThenInclude(l => l.GoodsReceiptDetail)
                        .ThenInclude(d => d!.GoodsReceipt)
                            .ThenInclude(gr => gr.Warehouse)
                .Include(b => b.Lot)
                    .ThenInclude(l => l.GoodsReceiptDetail)
                        .ThenInclude(d => d!.ProductVariant)
                            .ThenInclude(v => v.Product)
                .Include(b => b.Slot)
                .AsNoTracking()
                .Where(b => b.Lot.GoodsReceiptDetail.GoodsReceiptId == goodsReceiptId)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<Box>> GetAvailableBoxesForVariantAsync(int productVariantId, bool includeOfflineOnly = false)
        {
            var query = _context.Boxes
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
                .AsQueryable();

            if (!includeOfflineOnly)
            {
                // Online channel: hide boxes that ever had approved stock-check variance.
                // They can still be sold offline (POS) by calling with includeOfflineOnly=true.
                query = query.Where(b => !_context.StockCheckDetails.Any(d =>
                    d.BoxId == b.Id &&
                    d.StockCheck.Status == StockCheckStatus.Approved &&
                    d.VarianceType.HasValue &&
                    d.VarianceType != VarianceType.Match));
            }

            var utcNow = DateTime.UtcNow;
            query = WhereNotBlockedByOrderAllocation(query, _context, utcNow);

            return await query
                .OrderBy(b => b.Lot.ExpiryDate)
                .ToListAsync();
        }

        public async Task<decimal> GetTotalBoxWeightByLotIdAsync(int lotId)
        {
            // SumAsync trả về 0 nếu không có bản ghi (do cast về decimal?)
            // Dùng nullable để tránh exception khi sequence rỗng.
            var total = await _context.Boxes
                .Where(b => b.LotId == lotId)
                .SumAsync(b => (decimal?)b.Weight);
            return total ?? 0m;
        }

        public async Task<int> GetAvailableBoxCountByVariantIdAsync(int productVariantId)
        {
            var utcNow = DateTime.UtcNow;
            var query = _context.Boxes
                .Include(b => b.Lot)
                    .ThenInclude(l => l.GoodsReceiptDetail)
                .Where(b =>
                    b.Lot.GoodsReceiptDetail.ProductVariantId == productVariantId &&
                    b.Status == BoxStatus.Stored &&
                    b.Lot.Status == LotStatus.Active &&
                    b.Lot.ExpiryDate > utcNow);

            query = WhereNotBlockedByOrderAllocation(query, _context, utcNow);

            return await query.CountAsync();
        }

        public async Task<int> GetAvailableBoxCountByVariantAndTypeAsync(
            int productVariantId,
            bool isPartial,
            decimal weight,
            bool includeOfflineOnly = false)
        {
            var now = DateTime.UtcNow;

            // Query tối ưu: không Include toàn bộ entity, chỉ dùng navigation để filter ExpiryDate.
            var query = _context.Boxes
                .Where(b =>
                    b.Lot.GoodsReceiptDetail.ProductVariantId == productVariantId &&
                    b.Status == BoxStatus.Stored &&
                    b.Lot.Status == LotStatus.Active &&
                    b.IsPartial == isPartial &&
                    b.Weight == weight)
                .AsQueryable();

            if (!includeOfflineOnly)
            {
                query = query.Where(b => !_context.StockCheckDetails.Any(d =>
                    d.BoxId == b.Id &&
                    d.StockCheck.Status == StockCheckStatus.Approved &&
                    d.VarianceType.HasValue &&
                    d.VarianceType != VarianceType.Match));
            }

            query = WhereNotBlockedByOrderAllocation(query, _context, now);

            // 1) Kiểm tra nhanh xem có box hết hạn / ExpiryDate <= now không
            bool hasExpired = await query.AnyAsync(b => b.Lot.ExpiryDate <= now);
            if (hasExpired)
            {
                throw new InvalidBusinessRuleException("Có box thuộc loại này đã hết hạn hoặc ExpiryDate không hợp lệ.");
            }

            // 2) Đếm số box còn hạn
            return await query.CountAsync(b => b.Lot.ExpiryDate > now);
        }

        public async Task<decimal> GetTotalStockWeightByWarehouseIdAsync(int warehouseId)
        {
            var total = await _context.Boxes
                .Where(b =>
                    b.Lot.GoodsReceiptDetail.GoodsReceipt.WarehouseId == warehouseId &&
                    b.Status != BoxStatus.Exported)
                .SumAsync(b => (decimal?)b.Weight);

            return total ?? 0m;
        }

        public async Task<decimal> GetAssignedStockWeightByWarehouseIdAsync(int warehouseId)
        {
            var total = await _context.Boxes
                .Where(b =>
                    b.Lot.GoodsReceiptDetail.GoodsReceipt.WarehouseId == warehouseId &&
                    b.Status != BoxStatus.Exported &&
                    b.SlotId != null)
                .SumAsync(b => (decimal?)b.Weight);

            return total ?? 0m;
        }

        public async Task<decimal> GetUnassignedStockWeightByWarehouseIdAsync(int warehouseId)
        {
            var total = await _context.Boxes
                .Where(b =>
                    b.Lot.GoodsReceiptDetail.GoodsReceipt.WarehouseId == warehouseId &&
                    b.Status != BoxStatus.Exported &&
                    b.SlotId == null)
                .SumAsync(b => (decimal?)b.Weight);

            return total ?? 0m;
        }

        public async Task<List<BoxTypeSummary>> GetAvailableBoxTypeSummaryByVariantIdAsync(int productVariantId)
        {
            var utcNow = DateTime.UtcNow;
            var baseQuery = _context.Boxes
                .Include(b => b.Lot)
                    .ThenInclude(l => l.GoodsReceiptDetail)
                .Where(b =>
                    b.Lot.GoodsReceiptDetail.ProductVariantId == productVariantId &&
                    b.Status == BoxStatus.Stored &&
                    b.Lot.Status == LotStatus.Active &&
                    b.Lot.ExpiryDate > utcNow &&
                    !_context.StockCheckDetails.Any(d =>
                        d.BoxId == b.Id &&
                        d.StockCheck.Status == StockCheckStatus.Approved &&
                        d.VarianceType.HasValue &&
                        d.VarianceType != VarianceType.Match));

            var query = WhereNotBlockedByOrderAllocation(baseQuery, _context, utcNow);

            return await query
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
