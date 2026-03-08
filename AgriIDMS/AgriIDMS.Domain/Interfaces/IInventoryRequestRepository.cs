using AgriIDMS.Domain.Entities;
using System.Threading.Tasks;

namespace AgriIDMS.Domain.Interfaces
{
    public interface IInventoryRequestRepository
    {
        Task AddAsync(InventoryRequest request);
    }
}
