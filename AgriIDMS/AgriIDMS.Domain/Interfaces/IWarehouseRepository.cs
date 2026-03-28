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

        /// <summary>Tổng sức chứa (kg) của tất cả slot thuộc kho.</summary>
        Task<decimal> GetTotalCapacityByWarehouseIdAsync(int warehouseId);

        /// <summary>Tổng khối lượng đang nằm trong slot (kg) của kho.</summary>
        Task<decimal> GetTotalCurrentCapacityByWarehouseIdAsync(int warehouseId);

        /// <summary>Tổng dung lượng còn trống (Capacity - CurrentCapacity) của tất cả slot thuộc kho.</summary>
        Task<decimal> GetTotalRemainingCapacityByWarehouseIdAsync(int warehouseId);
    }
}
