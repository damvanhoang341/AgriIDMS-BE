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

        // ===== TỔNG PHÂN LOẠI =====
        public decimal? TotalEstimatedQuantity { get; set; }
        public decimal? TotalActualQuantity { get; set; }

        // ===== CHÊNH LỆCH =====
        public decimal? WeightDifference =>
            NetWeight.HasValue && TotalActualQuantity.HasValue
                ? NetWeight - TotalActualQuantity
                : null;
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
