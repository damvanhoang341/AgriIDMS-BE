using AgriIDMS.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AgriIDMS.Application.Interfaces
{
    public interface ILotService
    {
        Task<List<Lot>> GetLotsByGoodsReceiptIdAsync(int goodsReceiptId);
        Task<object?> GetByLotCodeAsync(string lotCode);
        /// <summary>Lưu URL ảnh QR (đã upload Cloudinary từ FE).</summary>
        Task UpdateQrImageUrlAsync(int lotId, string qrImageUrl);
    }
}
