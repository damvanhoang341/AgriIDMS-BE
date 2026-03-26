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
    }
}
