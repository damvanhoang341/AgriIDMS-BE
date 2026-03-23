using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgriIDMS.Application.DTOs.Lot
{
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

        public string Status { get; set; } = string.Empty; // NearExpiry / Expired
    }

    public class NearExpiryDashboardDto
    {
        public int DaysThreshold { get; set; }
        public int TotalLots { get; set; }
        public int TotalBoxes { get; set; }
        public IList<NearExpiryLotDto> Lots { get; set; } = new List<NearExpiryLotDto>();
    }
}
