using AgriIDMS.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AgriIDMS.Domain.Interfaces
{
    public interface IExportReceiptRepository
    {
        Task AddAsync(ExportReceipt receipt);
        Task<ExportReceipt?> GetByIdWithDetailsAsync(int id);
        Task<IEnumerable<ExportReceipt>> GetAllExport();
        Task<bool> ExistsForOrderAsync(int orderId);

        /// <summary>Phiếu ReadyToExport chờ Manager/Admin duyệt.</summary>
        Task<IList<ExportReceipt>> GetReadyToExportPendingApproveAsync(int skip, int take, string? sort);

        /// <summary>Phiếu xuất đã duyệt xuất kho thành công (ExportStatus.Approved).</summary>
        Task<IList<ExportReceipt>> GetApprovedExportsAsync(int skip, int take, string? sort);
    }
}
