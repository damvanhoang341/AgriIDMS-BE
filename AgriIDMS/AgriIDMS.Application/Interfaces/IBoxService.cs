using AgriIDMS.Application.DTOs.Box;
using System.Threading.Tasks;

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
    }
}
