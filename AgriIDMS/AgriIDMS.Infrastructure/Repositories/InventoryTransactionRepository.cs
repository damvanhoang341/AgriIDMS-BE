using AgriIDMS.Domain.Entities;
using AgriIDMS.Domain.Enums;
using AgriIDMS.Domain.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace AgriIDMS.Infrastructure.Repositories
{
    public class InventoryTransactionRepository : IInventoryTransactionRepository
    {
        private readonly AppDbContext _context;

        public InventoryTransactionRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task CreateAsync(InventoryTransaction transaction)
        {
            await _context.InventoryTransactions.AddAsync(transaction);
        }

        public async Task AddRangeAsync(IEnumerable<InventoryTransaction> transactions)
        {
            await _context.InventoryTransactions.AddRangeAsync(transactions);
        }

        public async Task<List<InventoryTransaction>> GetDisposeTransactionsAsync(
            int warehouseId,
            DateTime? fromDate,
            DateTime? toDate,
            string? createdByKeyword)
        {
            var query = _context.InventoryTransactions
                .Include(t => t.Box)
                    .ThenInclude(b => b.Lot)
                        .ThenInclude(l => l.GoodsReceiptDetail)
                            .ThenInclude(d => d!.GoodsReceipt)
                                .ThenInclude(gr => gr.Warehouse)
                .Include(t => t.Box)
                    .ThenInclude(b => b.Lot)
                        .ThenInclude(l => l.GoodsReceiptDetail)
                            .ThenInclude(d => d!.ProductVariant)
                                .ThenInclude(v => v.Product)
                .Include(t => t.CreatedUser)
                .Include(t => t.Box)
                    .ThenInclude(b => b.Slot)
                .AsNoTracking()
                .Where(t =>
                    t.TransactionType == InventoryTransactionType.Dispose &&
                    t.Box.Lot.GoodsReceiptDetail.GoodsReceipt.WarehouseId == warehouseId);

            if (fromDate.HasValue)
            {
                var fromUtc = DateTime.SpecifyKind(fromDate.Value.Date, DateTimeKind.Utc);
                query = query.Where(t => t.CreatedAt >= fromUtc);
            }

            if (toDate.HasValue)
            {
                var toUtcExclusive = DateTime.SpecifyKind(toDate.Value.Date.AddDays(1), DateTimeKind.Utc);
                query = query.Where(t => t.CreatedAt < toUtcExclusive);
            }

            if (!string.IsNullOrWhiteSpace(createdByKeyword))
            {
                var key = createdByKeyword.Trim().ToLower();
                query = query.Where(t =>
                    t.CreatedBy.ToLower().Contains(key) ||
                    (t.CreatedUser != null && (
                        (t.CreatedUser.UserName != null && t.CreatedUser.UserName.ToLower().Contains(key)) ||
                        (t.CreatedUser.FullName != null && t.CreatedUser.FullName.ToLower().Contains(key))
                    )));
            }

            return await query
                .OrderByDescending(t => t.CreatedAt)
                .ThenByDescending(t => t.Id)
                .Take(500)
                .ToListAsync();
        }
    }
}
