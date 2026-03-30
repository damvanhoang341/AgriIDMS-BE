using AgriIDMS.Domain.Entities;
using AgriIDMS.Domain.Enums;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AgriIDMS.Domain.Interfaces
{
    public interface IReviewRepository
    {
        Task AddAsync(Review review);
        Task<Review?> GetByOrderDetailIdAsync(int orderDetailId);
        Task<OrderDetail?> GetOrderDetailForReviewAsync(int orderDetailId);
        Task<bool> HasNonResolvedComplaintAsync(int orderId, int orderDetailId);
        Task<ComplaintStatus?> GetLatestComplaintStatusAsync(int orderId, int orderDetailId);
        Task<IList<Review>> GetApprovedByProductVariantAsync(int productVariantId, int skip, int take);
    }
}
