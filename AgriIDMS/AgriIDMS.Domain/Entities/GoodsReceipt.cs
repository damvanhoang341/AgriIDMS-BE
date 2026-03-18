using AgriIDMS.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AgriIDMS.Domain.Entities
{
    public class GoodsReceipt
    {
        public int Id { get; set; }

        public string ReceiptCode { get; set; } = null!;

        public int? PurchaseOrderId { get; set; }
        public PurchaseOrder? PurchaseOrder { get; set; }

        public int SupplierId { get; set; }
        public Supplier Supplier { get; set; } = null!;

        public int WarehouseId { get; set; }
        public Warehouse Warehouse { get; set; } = null!;

        public GoodsReceiptStatus Status { get; set; } = GoodsReceiptStatus.Draft;

        public string VehicleNumber { get; set; } = null!;
        public string? DriverName { get; set; }
        public string? TransportCompany { get; set; }

        /// <summary>Tổng khối lượng đặt (từ PO), từ Details.PurchaseOrderDetail.OrderedWeight.</summary>
        public decimal TotalExpectedWeight => Details.Sum(d => d.PurchaseOrderDetail?.OrderedWeight ?? 0);
        public decimal TotalReceivedWeight => Details.Sum(x => x.ReceivedWeight);
        public decimal TotalUsableWeight => Details.Sum(x => x.UsableWeight ?? 0m);

        public string CreatedBy { get; set; } = null!;
        public ApplicationUser CreatedUser { get; set; } = null!;
        public string? ReceivedBy { get; set; }
        public string? ApprovedBy { get; set; }
        public ApplicationUser? ApprovedUser { get; set; }

        public DateTime ReceivedDate { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ApprovedAt { get; set; }

        /// <summary>Lý do chuyển sang chờ Manager (vượt dung sai / dưới định mức). Hiển thị cho Manager xem xét Approve/Reject.</summary>
        public string? PendingReason { get; set; }

        public ICollection<GoodsReceiptDetail> Details { get; set; } = new List<GoodsReceiptDetail>();
    }
}
