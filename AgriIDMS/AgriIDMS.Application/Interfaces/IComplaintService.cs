using AgriIDMS.Application.DTOs.Complaint;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AgriIDMS.Application.Interfaces
{
    public interface IComplaintService
    {
        Task<ComplaintResponseDto> CreateAsync(CreateComplaintRequest request, string userId);
        Task<IReadOnlyList<ComplaintResponseDto>> GetMineAsync(string userId);
        Task<ComplaintResponseDto> GetByIdAsync(int complaintId, string userId);
        Task<ComplaintResponseDto> GetByIdForStaffAsync(int complaintId);
        Task<IReadOnlyList<ComplaintResponseDto>> GetAllForStaffAsync(int skip, int take);
        Task<ComplaintResponseDto> VerifyAsync(int complaintId, VerifyComplaintRequest request, string staffUserId);
        Task<ComplaintResponseDto> CancelPendingAsync(int complaintId, string userId);
    }
}
