using System;
using System.Collections.Generic;

namespace AgriIDMS.Application.DTOs.GoodsReceipt
{
    /// <summary>Dữ liệu in template HTML "Phiếu nhập kho".</summary>
    public class GoodsReceiptPrintDataDto
    {
        public string SchemaVersion { get; set; } = "1";

        /// <summary>Luôn là "Phiếu nhập kho".</summary>
        public string DocumentTitle { get; set; } = "Phiếu nhập kho";

        public DateTime SnapshotAtUtc { get; set; }

        /// <summary>afterQc | afterApprove | preview</summary>
        public string SnapshotPhase { get; set; } = null!;

        public bool IsPreview { get; set; }

        /// <summary>Phiếu đang chờ Manager (dung sai / định mức / …).</summary>
        public bool RequiresManagerAttention { get; set; }

        /// <summary>Cảnh báo hiển thị khi in (ví dụ chờ duyệt).</summary>
        public string? PrintWarningMessage { get; set; }

        /// <summary>FromPurchaseOrder | DirectInbound</summary>
        public string ReceiptType { get; set; } = null!;

        /// <summary>Lý do nhập không PO / ghi chú nghiệp vụ.</summary>
        public string? NonPoReason { get; set; }

        public int ReceiptId { get; set; }
        public string ReceiptCode { get; set; } = null!;
        public string ReceiptStatus { get; set; } = null!;

        public int? PurchaseOrderId { get; set; }
        public string? PurchaseOrderCode { get; set; }

        public string SupplierName { get; set; } = null!;
        public string WarehouseName { get; set; } = null!;

        public string VehicleNumber { get; set; } = null!;
        public string? DriverName { get; set; }
        public string? TransportCompany { get; set; }

        public DateTime ReceivedDate { get; set; }

        public decimal TotalReceivedWeight { get; set; }
        public decimal TotalUsableWeight { get; set; }

        public string? ApprovedByUserName { get; set; }
        public DateTime? ApprovedAtUtc { get; set; }

        public List<GoodsReceiptPrintLineDto> Lines { get; set; } = new();
    }

    public class GoodsReceiptPrintLineDto
    {
        public int LineNo { get; set; }
        public int DetailId { get; set; }
        public string ProductName { get; set; } = null!;
        public string Grade { get; set; } = null!;
        public decimal? OrderedWeightKg { get; set; }
        public decimal ReceivedWeightKg { get; set; }
        public decimal? UsableWeightKg { get; set; }
        public string QcResult { get; set; } = null!;
        public string? QcNote { get; set; }
        public string? InspectedBy { get; set; }
        public DateTime? InspectedAtUtc { get; set; }
    }
}
