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
    }
}
