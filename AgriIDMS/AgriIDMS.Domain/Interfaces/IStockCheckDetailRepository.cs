using AgriIDMS.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AgriIDMS.Domain.Interfaces
{
    public interface IStockCheckDetailRepository
    {
        Task AddRangeAsync(IEnumerable<StockCheckDetail> details);
        Task<StockCheckDetail?> GetByIdAsync(int id);
        Task UpdateAsync(StockCheckDetail detail);
    }
}
