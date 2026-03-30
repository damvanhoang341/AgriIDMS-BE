using AgriIDMS.Application.DTOs.Review;
using System.Threading.Tasks;

namespace AgriIDMS.Application.Interfaces
{
    public interface IReviewService
    {
        Task<ReviewResponseDto> CreateReviewAsync(CreateReviewRequest request, string customerId);
        Task ValidateReviewEligibility(int orderDetailId, string customerId);
        Task<bool> IsReviewableAsync(int orderDetailId, string customerId);
    }
}
