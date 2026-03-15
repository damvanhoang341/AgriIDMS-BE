using AgriIDMS.Application.DTOs.GoodsReceipt;
using System.Threading.Tasks;

namespace AgriIDMS.Application.Interfaces
{
    public interface IGoodsReceiptService
    {
        Task<int> CreateGoodsReceiptAsync(CreateGoodsReceiptRequest request, string userId);
        Task AddGoodsReceiptDetailAsync(AddGoodsReceiptDetailRequest request);
        Task UpdateTruckWeightAsync(UpdateTruckWeightRequest request);
        Task QCInspectionAsync(QCInspectionRequest request, string userId);
        Task GenerateBoxesAsync(CreateBoxesRequest request, string userId);
        Task ApproveGoodsReceiptAsync(int receiptId, string userId);
        Task ManagerApproveReceiptAsync(int receiptId, string userId);
        Task ManagerRejectReceiptAsync(int receiptId, string userId);
        Task GenerateLotAsync(int goodsReceiptDetailId);

        Task<IEnumerable<GoodsReceiptSummaryDto>> GetAllAsync();
        Task<GoodsReceiptResponseDto> GetByIdAsync(int id);
        /// <summary>Phiếu nhập kèm giá nhập, chỉ dùng cho màn duyệt phiếu (Manager/Admin).</summary>
        Task<GoodsReceiptForApprovalDto> GetByIdForApprovalAsync(int id);
    }
}
