using AgriIDMS.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgriIDMS.Domain.Interfaces
{
    public interface IBoxRepository
    {
        Task<Box?> GetByIdAsync(int id);
        Task<Box?> GetByIdWithLotAndReceiptAsync(int id);
        Task<Dictionary<int, Box>> GetByIdsAsync(IEnumerable<int> ids);
        /// <summary>Lấy nhiều box kèm Lot, GoodsReceiptDetail, GoodsReceipt, Slot (dùng cho batch assign).</summary>
        Task<List<Box>> GetByIdsWithLotAndReceiptAsync(IEnumerable<int> ids);
        Task CreateAsync(Box box);
        Task UpdateAsync(Box box);
        Task<List<Box>> GetAvailableBoxesForVariantAsync(int productVariantId, bool includeOfflineOnly = false);
        Task<Box?> GetByQrCodeAsync(string qrCode);
        /// <summary>Danh sách box được tạo từ một phiếu nhập.</summary>
        Task<List<Box>> GetByGoodsReceiptIdAsync(int goodsReceiptId);
        /// <summary>Danh sách box thuộc kho nhưng chưa được gán vào slot.</summary>
        Task<List<Box>> GetUnassignedBoxesByWarehouseIdAsync(int warehouseId);
        /// <summary>Danh sách box hư hỏng (có thể lọc theo kho).</summary>
        Task<List<Box>> GetDamagedBoxesAsync(int? warehouseId = null);
        /// <summary>Danh sách box hết hạn theo kho, còn tồn vật lý (weight &gt; 0, chưa xuất).</summary>
        Task<List<Box>> GetExpiredBoxesByWarehouseIdAsync(int warehouseId);
        Task<int> GetAvailableBoxCountByVariantIdAsync(int productVariantId);
        /// <summary>Lấy tổng hợp các loại box khả dụng (group theo IsPartial & Weight) cho 1 ProductVariant.</summary>
        Task<List<BoxTypeSummary>> GetAvailableBoxTypeSummaryByVariantIdAsync(int productVariantId);
        /// <summary>Đếm số box khả dụng theo đúng loại box (full/partial + weight) cho 1 ProductVariant.</summary>
        /// <param name="includeOfflineOnly">true = POS: gồm cả box từng lệch kiểm kê (bán offline).</param>
        Task<int> GetAvailableBoxCountByVariantAndTypeAsync(int productVariantId, bool isPartial, decimal weight, bool includeOfflineOnly = false);

        /// <summary>Tổng khối lượng box đã tạo cho 1 lot (dùng để chống tạo box vô hạn).</summary>
        Task<decimal> GetTotalBoxWeightByLotIdAsync(int lotId);

        /// <summary>Tổng khối lượng box của một kho (loại trừ box đã Exported).</summary>
        Task<decimal> GetTotalStockWeightByWarehouseIdAsync(int warehouseId);

        /// <summary>Tổng khối lượng box đã được xếp slot của một kho (loại trừ box đã Exported).</summary>
        Task<decimal> GetAssignedStockWeightByWarehouseIdAsync(int warehouseId);

        /// <summary>Tổng khối lượng box chưa xếp slot của một kho (loại trừ box đã Exported).</summary>
        Task<decimal> GetUnassignedStockWeightByWarehouseIdAsync(int warehouseId);
    }
}
