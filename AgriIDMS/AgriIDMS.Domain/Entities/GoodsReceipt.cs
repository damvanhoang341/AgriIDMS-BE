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

        /// <summary>Phân loại phiếu (theo PO / nhập trực tiếp). Hiển thị trên Phiếu nhập kho.</summary>
        public InboundReceiptKind InboundReceiptKind { get; set; } = InboundReceiptKind.FromPurchaseOrder;

        /// <summary>Lý do nhập không PO hoặc ghi chú nghiệp vụ (khi <see cref="InboundReceiptKind"/> = DirectInbound hoặc không có PO).</summary>
        public string? NonPoReason { get; set; }

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

        /// <summary>Snapshot JSON in Phiếu nhập kho sau khi QC xong (QCCompleted hoặc PendingManagerApproval).</summary>
        public string? PrintSnapshotAfterQcJson { get; set; }

        /// <summary>Snapshot JSON in Phiếu nhập kho sau khi duyệt nhập (Approved).</summary>
        public string? PrintSnapshotAfterApproveJson { get; set; }

        public ICollection<GoodsReceiptDetail> Details { get; set; } = new List<GoodsReceiptDetail>();
    }
}
