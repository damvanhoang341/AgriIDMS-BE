using AgriIDMS.Application.DTOs.Warehouse;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AgriIDMS.Application.Interfaces
{
    public interface IZoneService
    {
        Task<List<ZoneDto>> GetByWarehouseAsync(int warehouseId);
        Task<int> CreateAsync(int warehouseId, CreateZoneRequest request);
        Task UpdateAsync(int id, CreateZoneRequest request);
        Task DeleteAsync(int id);
    }
}
