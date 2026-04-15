using AgriIDMS.Application.DTOs.GoodsReceipt;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AgriIDMS.Application.Interfaces
{
    public interface IGoodsReceiptService
    {
        Task<int> CreateGoodsReceiptAsync(
            CreateGoodsReceiptRequest request,
            string userId,
            bool autoApproveWhenCreatedByManager = false);
        /// <summary>Admin/Manager: bỏ qua duyệt bước 1 (Draft → Received) khi đã có dòng chi tiết. Không QC, không duyệt nhập kho.</summary>
        Task ApplyPrivilegedFirstApprovalIfDraftAsync(int goodsReceiptId);
        Task QCInspectionAsync(QCInspectionRequest request, string userId);
        Task<IReadOnlyList<BoxCreatedItemDto>> GenerateBoxesAsync(CreateBoxesRequest request, string userId);
        Task ApproveGoodsReceiptAsync(int receiptId, string userId);
        Task ManagerReviewToleranceAsync(int receiptId, bool isApproved, string userId);
        Task ManagerReviewMinWeightAsync(int receiptId, bool isApproved, string userId);
        Task GenerateLotAsync(int goodsReceiptDetailId);
        Task UpdateWarehouseAsync(int receiptId, UpdateGoodsReceiptWarehouseRequest request, string userId);

        Task<IEnumerable<GoodsReceiptSummaryDto>> GetAllAsync();
        Task<GoodsReceiptResponseDto> GetByIdAsync(int id);
        /// <summary>Phiếu nhập kèm giá nhập, chỉ dùng cho màn duyệt phiếu (Manager/Admin).</summary>
        Task<GoodsReceiptForApprovalDto> GetByIdForApprovalAsync(int id);

        /// <param name="phase">afterQc | afterApprove — null = tự chọn (ưu tiên sau duyệt).</param>
        /// <param name="preview">true = xem trước từ dữ liệu hiện tại, không yêu cầu đã QC xong.</param>
        Task<GoodsReceiptPrintDataDto> GetGoodsReceiptPrintDataAsync(int receiptId, string? phase, bool preview);
    }
}
