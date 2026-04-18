using AgriIDMS.Domain.Entities;
using AgriIDMS.Domain.Enums;

namespace AgriIDMS.Domain.Interfaces
{
    public interface IDamageReportRepository
    {
        Task AddAsync(DamageReport item);
        Task<DamageReport?> GetByIdAsync(int id);
        Task<List<DamageReport>> GetListAsync(
            DamageReportStatus? status = null,
            int? warehouseId = null,
            string? reportedByUserId = null,
            DamageProcessingOutcome? requestedOutcome = null);

        /// <summary>Phiếu hỏng đang chờ duyệt cho thùng — dùng để chặn tồn bán.</summary>
        Task<bool> HasPendingForBoxAsync(int boxId);
    }
}

