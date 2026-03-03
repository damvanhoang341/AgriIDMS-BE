using AgriIDMS.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AgriIDMS.Domain.Interfaces
{
    public interface IZoneRepository
    {
        Task<List<Zone>> GetByWarehouseAsync(int warehouseId);
        Task<Zone?> GetByIdAsync(int id);
        Task<bool> ExistsByNameAsync(int warehouseId, string name);
        Task AddAsync(Zone zone);
        Task UpdateAsync(Zone zone);
        Task DeleteAsync(Zone zone);
    }
}

