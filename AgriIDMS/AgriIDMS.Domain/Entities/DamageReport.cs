using AgriIDMS.Domain.Enums;

namespace AgriIDMS.Domain.Entities
{
    public class DamageReport : BaseEntity
    {
        public DamageTargetType TargetType { get; set; } = DamageTargetType.Box;
        public int TargetId { get; set; }
        public string TargetCode { get; set; } = string.Empty;

        public int? ProductVariantId { get; set; }
        public ProductVariant? ProductVariant { get; set; }
        public string? ProductName { get; set; }

        public int? LotId { get; set; }
        public Lot? Lot { get; set; }
        public string? LotCode { get; set; }

        public int? WarehouseId { get; set; }
        public Warehouse? Warehouse { get; set; }
        public string? WarehouseName { get; set; }

        public string DamageReason { get; set; } = string.Empty;
        public decimal DamagePercent { get; set; }
        public decimal SuggestedDiscountPercent { get; set; }

        /// <summary>Loại xử lý đề xuất khi tạo phiếu (Manager duyệt phải khớp).</summary>
        public DamageProcessingOutcome? RequestedProcessingOutcome { get; set; }

        /// <summary>Khối lượng hỏng đề xuất (kg); null = hỏng hoàn toàn / toàn bộ thùng.</summary>
        public decimal? RequestedDamagedWeightKg { get; set; }
        public string? Note { get; set; }
        public string EvidenceImageUrl { get; set; } = string.Empty;

        public string ReportedByUserId { get; set; } = string.Empty;
        public ApplicationUser ReportedByUser { get; set; } = null!;
        public string ReportedByUsername { get; set; } = string.Empty;
        public DateTime ReportedAt { get; set; } = DateTime.UtcNow;

        public DamageReportStatus Status { get; set; } = DamageReportStatus.Pending;
        public string? ReviewedByUserId { get; set; }
        public ApplicationUser? ReviewedByUser { get; set; }
        public string? ReviewedByUsername { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public string? ReviewNote { get; set; }

        /// <summary>Kết quả xử lý sau duyệt (null khi chưa duyệt hoặc từ chối).</summary>
        public DamageProcessingOutcome? ProcessingOutcome { get; set; }

        /// <summary>Khối lượng hỏng đã duyệt loại (kg); với Complete = toàn bộ thùng tại thời điểm duyệt.</summary>
        public decimal? ApprovedDamagedWeightKg { get; set; }

        /// <summary>Snapshot trọng lượng thùng trước khi xử lý (audit).</summary>
        public decimal? BoxWeightSnapshotKg { get; set; }

        /// <summary>Giữ cột legacy; luồng mới không áp giảm giá — luôn null sau duyệt.</summary>
        public decimal? AppliedDiscountPercent { get; set; }
    }
}

