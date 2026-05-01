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

        public async Task<(decimal disposedKg, decimal stockAdjustmentLossKg)> GetLossSummaryAsync(
            DateTime? fromDate,
            DateTime? toDate,
            int? warehouseId,
            int? productId,
            int? productVariantId)
        {
            var query = _context.InventoryTransactions
                .AsNoTracking()
                .Where(t => t.TransactionType == InventoryTransactionType.Dispose
                            || t.TransactionType == InventoryTransactionType.Adjust);

            if (fromDate.HasValue)
                query = query.Where(t => t.CreatedAt >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(t => t.CreatedAt <= toDate.Value);

            if (warehouseId.HasValue && warehouseId.Value > 0)
            {
                query = query.Where(t =>
                    t.Box != null &&
                    t.Box.Lot != null &&
                    t.Box.Lot.GoodsReceiptDetail != null &&
                    t.Box.Lot.GoodsReceiptDetail.GoodsReceipt != null &&
                    t.Box.Lot.GoodsReceiptDetail.GoodsReceipt.WarehouseId == warehouseId.Value);
            }

            if (productId.HasValue && productId.Value > 0)
            {
                query = query.Where(t =>
                    t.Box != null &&
                    t.Box.Lot != null &&
                    t.Box.Lot.GoodsReceiptDetail != null &&
                    t.Box.Lot.GoodsReceiptDetail.ProductVariant != null &&
                    t.Box.Lot.GoodsReceiptDetail.ProductVariant.ProductId == productId.Value);
            }

            if (productVariantId.HasValue && productVariantId.Value > 0)
            {
                query = query.Where(t =>
                    t.Box != null &&
                    t.Box.Lot != null &&
                    t.Box.Lot.GoodsReceiptDetail != null &&
                    t.Box.Lot.GoodsReceiptDetail.ProductVariantId == productVariantId.Value);
            }

            var transactions = await query.ToListAsync();

            var disposedKg = transactions
                .Where(t => t.TransactionType == InventoryTransactionType.Dispose)
                .Sum(t => Math.Abs(t.Quantity));

            var stockAdjustmentLossKg = transactions
                .Where(t => t.TransactionType == InventoryTransactionType.Adjust && t.Quantity < 0)
                .Sum(t => Math.Abs(t.Quantity));

            return (disposedKg, stockAdjustmentLossKg);
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
