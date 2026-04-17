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
        public decimal? AppliedDiscountPercent { get; set; }
    }
}

