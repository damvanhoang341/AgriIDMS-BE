using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgriIDMS.Application.DTOs.GoodsReceipt
{
    public class CreateGoodsReceiptRequest
    {
        public int SupplierId { get; set; }
        public int WarehouseId { get; set; }

        public string VehicleNumber { get; set; } = null!;
        public string? DriverName { get; set; }
        public string? TransportCompany { get; set; }

        public decimal? GrossWeight { get; set; }
        public decimal? TareWeight { get; set; }

        public DateTime ReceivedDate { get; set; }

        public List<CreateGoodsReceiptDetailRequest> Details { get; set; }
            = new();
    }

    public class CreateGoodsReceiptDetailRequest
    {
        public int ProductVariantId { get; set; }
        public decimal EstimatedQuantity { get; set; }
        public decimal ActualQuantity { get; set; }
        public decimal UnitPrice { get; set; }
        public DateTime ExpiryDate { get; set; }
    }
}
