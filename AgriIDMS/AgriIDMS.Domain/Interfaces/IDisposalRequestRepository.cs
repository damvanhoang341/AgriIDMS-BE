using AgriIDMS.Domain.Entities;
using AgriIDMS.Domain.Enums;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AgriIDMS.Domain.Interfaces
{
    public interface IDisposalRequestRepository
    {
        Task<DisposalRequest?> GetByIdWithItemsAsync(int id);
        Task<List<DisposalRequest>> GetListAsync(DisposalRequestStatus? status, int? warehouseId);
        Task CreateAsync(DisposalRequest request);
    }
}

