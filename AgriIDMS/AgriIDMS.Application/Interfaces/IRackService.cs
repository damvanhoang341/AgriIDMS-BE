using AgriIDMS.Application.DTOs.Warehouse;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AgriIDMS.Application.Interfaces
{
    public interface IRackService
    {
        Task<List<RackDto>> GetByZoneAsync(int zoneId);
        Task<int> CreateAsync(int zoneId, CreateRackRequest request);
        Task UpdateAsync(int id, CreateRackRequest request);
        Task DeleteAsync(int id);
    }
}
