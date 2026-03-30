using AgriIDMS.Application.DTOs.Disposal;
using AgriIDMS.Domain.Enums;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AgriIDMS.Application.Interfaces
{
    public interface IDisposalRequestService
    {
        Task<int> CreateRequestAsync(CreateDisposalRequestDto dto, string userId);
        Task DirectDisposeAsync(CreateDisposalRequestDto dto, string reviewerUserId);
        Task<List<DisposalRequestListItemDto>> GetRequestsAsync(DisposalRequestStatus? status, int? warehouseId);
        Task<DisposalRequestDetailDto> GetRequestDetailAsync(int id);
        Task ApproveAsync(int id, string adminUserId, string? reviewNote = null);
        Task RejectAsync(int id, string adminUserId, string? reviewNote = null);
    }
}

