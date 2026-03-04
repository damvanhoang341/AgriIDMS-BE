using AgriIDMS.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AgriIDMS.Domain.Interfaces
{
    public interface ISlotRepository
    {
        Task<List<Slot>> GetByRackAsync(int rackId);
        Task<Slot?> GetByIdAsync(int id);
        Task AddAsync(Slot slot);
        Task UpdateAsync(Slot slot);
        Task DeleteAsync(Slot slot);
    }
}

