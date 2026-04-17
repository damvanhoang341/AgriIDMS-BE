using AgriIDMS.Application.DTOs.DamageReport;
using AgriIDMS.Domain.Enums;

namespace AgriIDMS.Application.Interfaces
{
    public interface IDamageReportService
    {
        Task<DamageReportResponseDto> CreateAsync(CreateDamageReportRequest request, string userId, string username);
        Task<IReadOnlyList<DamageReportResponseDto>> GetListAsync(DamageReportStatus? status = null);
        Task<DamageReportResponseDto> ApproveAsync(int id, ApproveDamageReportRequest request, string reviewerUserId, string reviewerUsername);
        Task<DamageReportResponseDto> RejectAsync(int id, RejectDamageReportRequest request, string reviewerUserId, string reviewerUsername);
    }
}

