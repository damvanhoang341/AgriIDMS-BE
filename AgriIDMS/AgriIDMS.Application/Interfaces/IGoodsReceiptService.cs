using AgriIDMS.Application.DTOs.GoodsReceipt;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgriIDMS.Application.Interfaces
{
    public interface IGoodsReceiptService
    {
        /// <summary>
        /// Tạo phiếu nhập kho (Draft)
        /// </summary>
        Task<int> CreateGoodsReceiptAsync(CreateGoodsReceiptRequest request, string userId);

        /// <summary>
        /// Thêm sản phẩm vào phiếu nhập
        /// </summary>
        Task AddGoodsReceiptDetailAsync(AddGoodsReceiptDetailRequest request);

        /// <summary>
        /// Cập nhật trọng lượng xe
        /// </summary>
        Task UpdateTruckWeightAsync(UpdateTruckWeightRequest request);

        /// <summary>
        /// QC kiểm tra chất lượng nông sản
        /// </summary>
        Task QCInspectionAsync(QCInspectionRequest request, string userId);

        /// <summary>
        /// Tạo Lot sau khi QC
        /// </summary>
        Task GenerateLotAsync(int goodsReceiptDetailId);

        /// <summary>
        /// System generate Box từ Lot
        /// </summary>
        Task GenerateBoxesAsync(CreateBoxesRequest request);

        /// <summary>
        /// Duyệt phiếu nhập kho → tạo InventoryTransaction
        /// </summary>
        Task ApproveGoodsReceiptAsync(int receiptId, string userId);
    }
}
