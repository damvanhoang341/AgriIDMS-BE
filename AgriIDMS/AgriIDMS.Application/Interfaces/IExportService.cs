using AgriIDMS.Application.DTOs.Export;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AgriIDMS.Application.Interfaces
{
    public interface IExportService
    {
        Task<ExportReceiptResponseDto> CreateExportReceiptAsync(int orderId, string userId);
        Task<ExportReceiptResponseDto> ConfirmPickAsync(int exportId, string userId);
        Task<ExportReceiptResponseDto> ApproveExportAsync(int exportId, string userId);
        Task<ExportReceiptResponseDto> CancelExportAsync(int exportId, string userId);
        Task<ExportReceiptResponseDto> GetExportReceiptAsync(int exportId);
        Task<IEnumerable<ExportReceiptResponseDto>> GetAllExport();
        Task<IList<PendingApproveExportListItemDto>> GetPendingApproveExportsAsync(GetPendingApproveExportsQuery query);
    }
}
