using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgriIDMS.Application.DTOs.Lot
{
    public class LotListItemDto
    {
        public int LotId { get; set; }
        public string LotCode { get; set; } = string.Empty;
        public string? QrImageUrl { get; set; }
        public decimal TotalQuantity { get; set; }
        public decimal RemainingQuantity { get; set; }
        public DateTime ReceivedDate { get; set; }
        public DateTime ExpiryDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public int GoodsReceiptId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string ProductVariantName { get; set; } = string.Empty;
        public string WarehouseName { get; set; } = string.Empty;
    }

    public class NearExpiryBoxDto
    {
        public int BoxId { get; set; }
        public string BoxCode { get; set; } = string.Empty;
        public decimal Weight { get; set; }
        public bool IsPartial { get; set; }
        public string Status { get; set; } = string.Empty;
        public int? SlotId { get; set; }
        public string? SlotCode { get; set; }
    }

    public class NearExpiryLotDto
    {
        public int LotId { get; set; }
        public string LotCode { get; set; } = string.Empty;

        public int ProductVariantId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string Grade { get; set; } = string.Empty;

        public decimal RemainingQuantity { get; set; }

        public DateTime ExpiryDate { get; set; }
        public int DaysLeft { get; set; }
        public int NearExpiryBoxCount { get; set; }
        public IList<NearExpiryBoxDto> Boxes { get; set; } = new List<NearExpiryBoxDto>();
        public int WarehouseId { get; set; }
        public string WarehouseName { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty; // NearExpiry / Expired

        /// <summary>
        /// % giảm giá đề xuất theo rule cấu hình (không phải giá bán thực tế).
        /// </summary>
        public decimal SuggestedDiscountPercent { get; set; }
    }

    public class NearExpiryDashboardDto
    {
        public int DaysThreshold { get; set; }
        public int TotalLots { get; set; }
        public int TotalBoxes { get; set; }
        public IList<NearExpiryLotDto> Lots { get; set; } = new List<NearExpiryLotDto>();
    }

    public class LotBoxItemDto
    {
        public int BoxId { get; set; }
        public string BoxCode { get; set; } = string.Empty;
        public decimal Weight { get; set; }
        public string Status { get; set; } = string.Empty;
        public int? SlotId { get; set; }
        public string? SlotCode { get; set; }
        public string? QrCode { get; set; }
        public string? QrImageUrl { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class LotDetailDto
    {
        public int LotId { get; set; }
        public string LotCode { get; set; } = string.Empty;
        public string? QrImageUrl { get; set; }
        public decimal TotalQuantity { get; set; }
        public decimal RemainingQuantity { get; set; }
        public DateTime ReceivedDate { get; set; }
        public DateTime ExpiryDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public int GoodsReceiptId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string ProductVariantName { get; set; } = string.Empty;
        public string WarehouseName { get; set; } = string.Empty;
        public IList<LotBoxItemDto> Boxes { get; set; } = new List<LotBoxItemDto>();
    }
}
