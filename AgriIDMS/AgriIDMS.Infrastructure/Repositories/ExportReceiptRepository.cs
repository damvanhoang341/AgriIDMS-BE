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
                .Include(e => e.Details)
                    .ThenInclude(d => d.Box)
                        .ThenInclude(b => b.Slot)
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
    }
}
