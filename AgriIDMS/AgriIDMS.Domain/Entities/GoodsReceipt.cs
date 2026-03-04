using AgriIDMS.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgriIDMS.Domain.Entities
{
    public class GoodsReceipt
    {
        public int Id { get; set; }

        public int SupplierId { get; set; }
        public Supplier Supplier { get; set; } = null!;

        public int WarehouseId { get; set; }
        public Warehouse Warehouse { get; set; } = null!;

        public GoodsReceiptStatus Status { get; set; } = GoodsReceiptStatus.Draft;
        // ================ đơn vị vận tải ===============
        public string VehicleNumber { get; set; } = null!;
        public string? DriverName { get; set; }
        public string? TransportCompany { get; set; }  

        // ===== CÂN XE =====
        public decimal? GrossWeight { get; set; }     // Cân xe đầy
        public decimal? TareWeight { get; set; }      // Cân xe rỗng
        public decimal? NetWeight =>GrossWeight.HasValue && TareWeight.HasValue? GrossWeight - TareWeight: null;

        // ===== TỔNG TÍNH TỪ DETAIL =====
        public decimal TotalOrderedWeight =>
            Details.Sum(x => x.OrderedWeight);

        public decimal TotalUsableWeight =>
            Details.Sum(x => x.UsableWeight ?? 0);

        // Hao hụt vận chuyển
        public decimal TransportLossWeight => NetWeight.HasValue ? NetWeight.Value - TotalOrderedWeight : 0;

        // Hao hụt QC
        public decimal QCLossWeight => TotalOrderedWeight - TotalUsableWeight;

        public decimal TotalLossWeight { get; private set; }
        public void CalculateTotalLossWeight()
        {
            TotalLossWeight = TransportLossWeight + QCLossWeight;
        }

        public decimal TolerancePercent { get; set; }   // dung sai
        public decimal AllowedLossWeight => TotalOrderedWeight * (TolerancePercent / 100);

        public decimal ClaimableWeight =>TotalLossWeight > AllowedLossWeight ? TotalLossWeight - AllowedLossWeight: 0;    // Phần vượt dung sai

        // ===== THÔNG TIN NGƯỜI TẠO =====
        public string CreatedBy { get; set; } = null!;
        public ApplicationUser CreatedUser { get; set; } = null!;

        public string? ApprovedBy { get; set; }
        public ApplicationUser? ApprovedUser { get; set; }

        public DateTime ReceivedDate { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ApprovedAt { get; set; }

        public ICollection<GoodsReceiptDetail> Details { get; set; }
            = new List<GoodsReceiptDetail>();
    }
}
