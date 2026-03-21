using AgriIDMS.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AgriIDMS.Domain.Interfaces
{
    public interface IComplaintRepository
    {
        Task AddAsync(Complaint complaint);
        Task<Complaint?> GetByIdAsync(int id);
        Task<Complaint?> GetByIdWithDetailsAsync(int id);
        Task<bool> HasPendingComplaintForOrderAndBoxAsync(int orderId, int boxId);
        Task<List<Complaint>> ListByUserIdAsync(string userId);
        Task<List<Complaint>> ListAllAsync(int skip, int take);
    }
}
