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
    }
}
