using AgriIDMS.Domain.Entities;
using AgriIDMS.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        public async Task CreadAsyn(InventoryTransaction transaction)
        {
            await _context.InventoryTransactions.AddAsync(transaction);
        }
    }
}
