using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgriIDMS.Application.DTOs.Lot
{
    public class NearExpiryLotDto
    {
        public int LotId { get; set; }
        public string LotCode { get; set; }

        public int ProductVariantId { get; set; }
        public string ProductName { get; set; }
        public string Grade { get; set; }

        public decimal RemainingQuantity { get; set; }

        public DateTime ExpiryDate { get; set; }
        public int DaysLeft { get; set; }

        public string Status { get; set; } // NearExpiry / Expired
    }
}
