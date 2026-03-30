using AgriIDMS.Application.DTOs.Lot;
using AgriIDMS.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AgriIDMS.Application.Interfaces
{
    public interface ILotService
    {
        Task<List<LotListItemDto>> GetAllLotsAsync();
        Task<LotDetailDto> GetLotDetailAsync(int lotId);
        Task<List<Lot>> GetLotsByGoodsReceiptIdAsync(int goodsReceiptId);
        Task<object?> GetByLotCodeAsync(string lotCode);
        /// <summary>Lưu URL ảnh QR (đã upload Cloudinary từ FE).</summary>
        Task UpdateQrImageUrlAsync(int lotId, string qrImageUrl);
        Task<IEnumerable<NearExpiryLotDto>> GetNearExpiryLotsAsync();
        Task<NearExpiryDashboardDto> GetNearExpiryDashboardAsync(int days, int? warehouseId = null);

        Task<List<NearExpiryDiscountRuleDto>> GetNearExpiryDiscountRulesAsync();
        Task UpdateNearExpiryDiscountRulesAsync(string userId, List<UpsertNearExpiryDiscountRuleDto> rules);
    }
}
