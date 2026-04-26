using AgriIDMS.Domain.Entities;
using AgriIDMS.Domain.Enums;
using AgriIDMS.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AgriIDMS.Infrastructure.Repositories
{
    public class ExportReceiptRepository : IExportReceiptRepository
    {
        private readonly AppDbContext _context;

        public ExportReceiptRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(ExportReceipt receipt)
        {
            await _context.ExportReceipts.AddAsync(receipt);
        }

        public async Task<ExportReceipt?> GetByIdWithDetailsAsync(int id)
        {
            return await _context.ExportReceipts
                .Include(e => e.Order)
                    .ThenInclude(o => o.Payments)
                .Include(e => e.Details)
                    .ThenInclude(d => d.Box)
                        .ThenInclude(b => b.Slot)
                .FirstOrDefaultAsync(e => e.Id == id);
        }

        public async Task<ExportReceipt?> GetByIdWithDetailsForPrintAsync(int id)
        {
            return await _context.ExportReceipts
                .AsSplitQuery()
                .Include(e => e.Order)
                    .ThenInclude(o => o.Details)
                .Include(e => e.Order)
                    .ThenInclude(o => o.Allocations)
                        .ThenInclude(a => a.OrderDetail)
                .Include(e => e.Details)
                    .ThenInclude(d => d.Box!)
                        .ThenInclude(b => b.Lot)
                            .ThenInclude(l => l.GoodsReceiptDetail)
                                .ThenInclude(grd => grd.ProductVariant)
                                    .ThenInclude(pv => pv.Product)
                .FirstOrDefaultAsync(e => e.Id == id);
        }

        public async Task<bool> ExistsForOrderAsync(int orderId)
        {
            return await _context.ExportReceipts
                .AnyAsync(e => e.OrderId == orderId
                    && e.Status != Domain.Enums.ExportStatus.Cancelled);
        }

        public async Task<IEnumerable<ExportReceipt>> GetAllExport()
        {
            return await _context.ExportReceipts.ToListAsync();
        }

        public async Task<IList<ExportReceipt>> GetReadyToExportPendingApproveAsync(int skip, int take, string? sort)
        {
            var q = _context.ExportReceipts
                .Include(e => e.Order)
                .Include(e => e.Details)
                .Where(e => e.Status == ExportStatus.ReadyToExport);

            var sortKey = sort?.Trim();
            if (string.Equals(sortKey, "createdAtAsc", StringComparison.OrdinalIgnoreCase))
                q = q.OrderBy(e => e.CreatedAt);
            else
                q = q.OrderByDescending(e => e.CreatedAt);

            return await q
                .Skip(skip)
                .Take(take)
                .ToListAsync();
        }

        public async Task<IList<ExportReceipt>> GetApprovedExportsAsync(int skip, int take, string? sort)
        {
            var q = _context.ExportReceipts
                .Include(e => e.Order)
                .Include(e => e.Details)
                .Where(e => e.Status == ExportStatus.Approved);

            var sortKey = sort?.Trim();
            if (string.Equals(sortKey, "createdAtAsc", StringComparison.OrdinalIgnoreCase))
                q = q.OrderBy(e => e.CreatedAt);
            else
                q = q.OrderByDescending(e => e.CreatedAt);

            return await q
                .Skip(skip)
                .Take(take)
                .ToListAsync();
        }

        public async Task<IList<ExportReceipt>> GetApprovedExportsForRevenueReportAsync(
            DateTime? fromDate,
            DateTime? toDate,
            int? warehouseId,
            int? productId,
            int? productVariantId)
        {
            var q = _context.ExportReceipts
                .AsSplitQuery()
                .Include(e => e.Order)
                    .ThenInclude(o => o.Allocations)
                        .ThenInclude(a => a.OrderDetail)
                .Include(e => e.Details)
                    .ThenInclude(d => d.Box)
                        .ThenInclude(b => b.Lot)
                            .ThenInclude(l => l.ProductVariant)
                                .ThenInclude(pv => pv.Product)
                .Include(e => e.Details)
                    .ThenInclude(d => d.Box)
                        .ThenInclude(b => b.Lot)
                            .ThenInclude(l => l.GoodsReceiptDetail)
                                .ThenInclude(grd => grd.GoodsReceipt)
                                    .ThenInclude(gr => gr.Warehouse)
                .Where(e => e.Status == ExportStatus.Approved);

            if (fromDate.HasValue)
                q = q.Where(e => e.CreatedAt >= fromDate.Value);

            if (toDate.HasValue)
                q = q.Where(e => e.CreatedAt <= toDate.Value);

            if (warehouseId.HasValue && warehouseId.Value > 0)
            {
                q = q.Where(e => e.Details.Any(d =>
                    d.Box != null &&
                    d.Box.Lot != null &&
                    d.Box.Lot.GoodsReceiptDetail != null &&
                    d.Box.Lot.GoodsReceiptDetail.GoodsReceipt != null &&
                    d.Box.Lot.GoodsReceiptDetail.GoodsReceipt.WarehouseId == warehouseId.Value));
            }

            if (productId.HasValue && productId.Value > 0)
            {
                q = q.Where(e => e.Details.Any(d =>
                    d.Box != null &&
                    d.Box.Lot != null &&
                    d.Box.Lot.ProductVariant != null &&
                    d.Box.Lot.ProductVariant.ProductId == productId.Value));
            }

            if (productVariantId.HasValue && productVariantId.Value > 0)
            {
                q = q.Where(e => e.Details.Any(d =>
                    d.Box != null &&
                    d.Box.Lot != null &&
                    d.Box.Lot.ProductVariantId == productVariantId.Value));
            }

            return await q
                .OrderByDescending(e => e.CreatedAt)
                .ToListAsync();
        }
    }
}
