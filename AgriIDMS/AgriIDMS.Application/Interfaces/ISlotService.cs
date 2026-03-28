using AgriIDMS.Application.DTOs.Warehouse;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AgriIDMS.Application.Interfaces
{
    public interface ISlotService
    {
        Task<List<SlotDto>> GetByRackAsync(int rackId);
        Task<int> CreateAsync(int rackId, CreateSlotRequest request);
        Task UpdateAsync(int id, CreateSlotRequest request);
        Task DeleteAsync(int id);
        Task<SlotDto?> GetByQrCodeAsync(string qrCode);
        Task<SlotContentsDto> GetContentsAsync(int slotId);
        /// <summary>Lưu URL ảnh QR (đã upload Cloudinary từ FE).</summary>
        Task UpdateQrImageUrlAsync(int slotId, string qrImageUrl);
        /// <summary>Đồng bộ CurrentCapacity của toàn bộ slot trong kho từ dữ liệu box thực tế.</summary>
        Task<int> SyncSlotCapacitiesByWarehouseAsync(int warehouseId);
    }
}
