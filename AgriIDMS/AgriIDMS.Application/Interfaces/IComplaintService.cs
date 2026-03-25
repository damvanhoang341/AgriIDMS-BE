using AgriIDMS.Application.DTOs.Complaint;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AgriIDMS.Application.Interfaces
{
    public interface IComplaintService
    {
        Task<ComplaintResponseDto> CreateAsync(CreateComplaintRequest request, string userId);
        Task<IReadOnlyList<ComplaintResponseDto>> GetMineAsync(string userId);
        /// <summary>
        /// Lấy danh sách box của đơn có thể khiếu nại (order thuộc user hiện tại + status Shipping/Completed).
        /// Dùng cho trang chọn box khi tạo complaint.
        /// </summary>
        Task<IReadOnlyList<ComplaintableBoxListItemDto>> GetOrderBoxesForComplaintAsync(int orderId, string userId);
        /// <summary>
        /// Lấy danh sách đơn (Shipping/Completed) của customer có thể khiếu nại + số lượng box eligible.
        /// Dùng cho trang /my-complaints để click theo đơn.
        /// </summary>
        Task<IReadOnlyList<EligibleOrderForComplaintListItemDto>> GetEligibleOrdersForCustomerAsync(string userId, int skip, int take);
        Task<ComplaintResponseDto> GetByIdAsync(int complaintId, string userId);
        Task<ComplaintResponseDto> GetByIdForStaffAsync(int complaintId);
        Task<IReadOnlyList<ComplaintResponseDto>> GetAllForStaffAsync(int skip, int take);
        Task<ComplaintResponseDto> VerifyAsync(int complaintId, VerifyComplaintRequest request, string staffUserId);
        Task<ComplaintResponseDto> CancelPendingAsync(int complaintId, string userId);
    }
}
