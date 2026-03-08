using AgriIDMS.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AgriIDMS.Domain.Interfaces
{
    public interface IStockCheckRepository
    {
        Task AddAsync(StockCheck stockCheck);
        Task<StockCheck?> GetByIdAsync(int id);
        Task<StockCheck?> GetByIdWithDetailsAndBoxesAsync(int id);
        Task UpdateAsync(StockCheck stockCheck);
        /// <summary>Lấy danh sách BoxId thuộc kho (box có Slot trong kho).</summary>
        Task<List<int>> GetBoxIdsInWarehouseAsync(int warehouseId);
    }
}
