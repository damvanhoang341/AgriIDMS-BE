using AgriIDMS.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AgriIDMS.Domain.Interfaces
{
    public interface IWarehouseRepository
    {
        Task<Warehouse?> GetWarehouseByIdAsync(int warehouseId);

        Task<bool> ExistsByNameAsync(string name);

        Task AddAsync(Warehouse warehouse);

        Task<List<Warehouse>> GetAllAsync();

        Task UpdateAsync(Warehouse warehouse);

        Task DeleteAsync(Warehouse warehouse);

        /// <summary>Tổng dung lượng còn trống (Capacity - CurrentCapacity) của tất cả slot thuộc kho.</summary>
        Task<decimal> GetTotalRemainingCapacityByWarehouseIdAsync(int warehouseId);
    }
}
