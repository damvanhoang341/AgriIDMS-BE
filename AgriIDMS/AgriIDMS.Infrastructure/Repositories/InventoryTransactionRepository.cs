using AgriIDMS.Domain.Entities;
using AgriIDMS.Domain.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

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
    }
}
