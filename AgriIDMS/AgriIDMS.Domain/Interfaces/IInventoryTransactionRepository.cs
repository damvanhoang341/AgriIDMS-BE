using AgriIDMS.Domain.Entities;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;

namespace AgriIDMS.Domain.Interfaces
{
    public interface IInventoryTransactionRepository
    {
        Task CreateAsync(InventoryTransaction transaction);
        Task AddRangeAsync(IEnumerable<InventoryTransaction> transactions);
        Task<(decimal disposedKg, decimal stockAdjustmentLossKg)> GetLossSummaryAsync(
            DateTime? fromDate,
            DateTime? toDate,
            int? warehouseId,
            int? productId,
            int? productVariantId);
        Task<List<InventoryTransaction>> GetDisposeTransactionsAsync(
            int warehouseId,
            DateTime? fromDate,
            DateTime? toDate,
            string? createdByKeyword);
    }
}
