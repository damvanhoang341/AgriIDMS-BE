using AgriIDMS.Application.DTOs.Warehouse;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AgriIDMS.Application.Interfaces
{
    public interface IWarehouseService
    {
        Task<int> CreateAsync(CreateWarehouseRequest request);
        Task<List<WarehouseDto>> GetAllAsync();
        Task<WarehouseDto> GetByIdAsync(int id);
        Task UpdateAsync(int id, CreateWarehouseRequest request);
        Task DeleteAsync(int id);
    }
}
