using AgriIDMS.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AgriIDMS.Domain.Interfaces
{
    public interface IInventoryTransactionRepository
    {
        Task CreateAsync(InventoryTransaction transaction);
        Task AddRangeAsync(IEnumerable<InventoryTransaction> transactions);
    }
}
