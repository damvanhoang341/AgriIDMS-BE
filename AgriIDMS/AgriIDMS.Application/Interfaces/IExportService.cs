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
        /// <param name="isManagerOrAdmin">Quản lý/Admin: được hủy cả phiếu <c>ReadyToExport</c>; kho chỉ hủy khi <c>PendingPick</c>.</param>
        Task<ExportReceiptResponseDto> CancelExportAsync(int exportId, string userId, bool isManagerOrAdmin);
        Task<ExportReceiptResponseDto> GetExportReceiptAsync(int exportId);
        /// <summary>Dữ liệu in phiếu xuất (FE template HTML). Snapshot chốt khi ReadyToExport; PendingPick trả preview.</summary>
        Task<ExportPrintDataDto> GetExportPrintDataAsync(int exportId);
        Task<IEnumerable<ExportReceiptResponseDto>> GetAllExport();
        Task<IList<PendingApproveExportListItemDto>> GetPendingApproveExportsAsync(GetPendingApproveExportsQuery query);

        /// <summary>Phiếu xuất đã duyệt (Approved) — lịch sử xuất thành công.</summary>
        Task<IList<PendingApproveExportListItemDto>> GetApprovedExportsAsync(GetPendingApproveExportsQuery query);
    }
}
