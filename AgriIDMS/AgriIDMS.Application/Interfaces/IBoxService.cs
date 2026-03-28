using AgriIDMS.Application.DTOs.Box;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace AgriIDMS.Application.Interfaces
{
    public interface IBoxService
    {
        Task AssignBoxToSlotAsync(AssignBoxToSlotRequest request);
        /// <summary>Gán nhiều box vào cùng một slot trong một lần (nhanh hơn gọi từng box).</summary>
        Task AssignBoxesToSlotAsync(AssignBoxesToSlotRequest request);
        /// <summary>Chuyển box đã xếp từ slot hiện tại sang slot khác (cùng kho) và ghi InventoryTransactionType.Transfer.</summary>
        Task TransferBoxToSlotAsync(TransferBoxToSlotRequest request, string userId);
        Task<object?> GetByQrCodeAsync(string qrCode);
        Task UpdateQrCodeAsync(int boxId, string? qrCode);
        /// <summary>Lưu URL ảnh QR (đã upload Cloudinary từ FE).</summary>
        Task UpdateQrImageUrlAsync(int boxId, string qrImageUrl);

        /// <summary>Danh sách box thuộc kho nhưng chưa được gán vào slot.</summary>
        Task<List<UnassignedBoxDto>> GetUnassignedBoxesByWarehouseAsync(int warehouseId);
        /// <summary>Danh sách box thuộc một phiếu nhập (để hiển thị QR box).</summary>
        Task<List<UnassignedBoxDto>> GetBoxesByGoodsReceiptAsync(int goodsReceiptId);
        /// <summary>Danh sách box hư hỏng (có thể lọc theo kho).</summary>
        Task<List<UnassignedBoxDto>> GetDamagedBoxesAsync(int? warehouseId = null);
        /// <summary>Danh sách box hết hạn trong kho (dành cho tiêu hủy).</summary>
        Task<List<UnassignedBoxDto>> GetExpiredBoxesByWarehouseAsync(int warehouseId);
        /// <summary>Tiêu hủy hàng hết hạn và lưu transaction.</summary>
        Task<DisposeExpiredBoxesResultDto> DisposeExpiredBoxesAsync(DisposeExpiredBoxesRequest request, string userId);
        /// <summary>Lịch sử tiêu hủy box trong kho, đọc từ InventoryTransactions.</summary>
        Task<List<DisposeHistoryItemDto>> GetDisposeHistoryAsync(
            int warehouseId,
            DateTime? fromDate,
            DateTime? toDate,
            string? createdByKeyword);
    }
}
