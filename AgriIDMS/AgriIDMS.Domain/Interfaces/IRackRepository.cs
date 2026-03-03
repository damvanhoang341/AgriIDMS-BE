using AgriIDMS.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AgriIDMS.Domain.Interfaces
{
    public interface IRackRepository
    {
        Task<List<Rack>> GetByZoneAsync(int zoneId);
        Task<Rack?> GetByIdAsync(int id);
        Task<bool> ExistsByNameAsync(int zoneId, string name);
        Task AddAsync(Rack rack);
        Task UpdateAsync(Rack rack);
        Task DeleteAsync(Rack rack);
    }
}

