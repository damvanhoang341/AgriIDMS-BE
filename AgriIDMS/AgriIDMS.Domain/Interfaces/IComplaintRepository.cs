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
        /// <summary>Lấy các BoxId đang có khiếu nại Pending cho một Order.</summary>
        Task<HashSet<int>> GetPendingComplaintBoxIdsForOrderAsync(int orderId);
        Task<List<Complaint>> ListByUserIdAsync(string userId);
        Task<List<Complaint>> ListAllAsync(int skip, int take);
    }
}
