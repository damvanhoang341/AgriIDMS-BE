using AgriIDMS.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace AgriIDMS.Application.DTOs.DamageReport
{
    public class CreateDamageReportRequest
    {
        [Required]
        public DamageTargetType TargetType { get; set; }

        [Range(1, int.MaxValue)]
        public int TargetId { get; set; }

        [Required]
        [MaxLength(100)]
        public string TargetCode { get; set; } = string.Empty;

        public int? ProductVariantId { get; set; }
        [MaxLength(200)]
        public string? ProductName { get; set; }

        public int? LotId { get; set; }
        [MaxLength(100)]
        public string? LotCode { get; set; }

        public int? WarehouseId { get; set; }
        [MaxLength(200)]
        public string? WarehouseName { get; set; }

        [Required]
        [MaxLength(500)]
        public string DamageReason { get; set; } = string.Empty;

        [Range(0, 100)]
        public decimal DamagePercent { get; set; }

        [Range(0, 100)]
        public decimal SuggestedDiscountPercent { get; set; }

        /// <summary>Loại hỏng đề xuất khi gửi phiếu.</summary>
        [Required]
        public DamageProcessingOutcome RequestedProcessingOutcome { get; set; }

        /// <summary>Khi <see cref="RequestedProcessingOutcome"/> = PartialDamaged — kg hỏng (bắt buộc).</summary>
        public decimal? RequestedDamagedWeightKg { get; set; }

        [MaxLength(1000)]
        public string? Note { get; set; }

        [Required]
        [MaxLength(500)]
        public string EvidenceImageUrl { get; set; } = string.Empty;
    }

    public class ApproveDamageReportRequest
    {
        [Required]
        public DamageProcessingOutcome Outcome { get; set; }

        /// <summary>Chỉ dùng khi <see cref="Outcome"/> = PartialDamaged — kg hỏng (phần còn tốt hệ thống tự tính).</summary>
        public decimal? DamagedWeightKg { get; set; }

        [MaxLength(1000)]
        public string? ReviewNote { get; set; }
    }

    public class RejectDamageReportRequest
    {
        [Required]
        [MinLength(1)]
        [MaxLength(1000)]
        public string ReviewNote { get; set; } = string.Empty;
    }

    public class DamageReportResponseDto
    {
        public int Id { get; set; }
        public string TargetType { get; set; } = string.Empty;
        public int TargetId { get; set; }
        public string TargetCode { get; set; } = string.Empty;
        public int? ProductVariantId { get; set; }
        public string? ProductName { get; set; }
        public int? LotId { get; set; }
        public string? LotCode { get; set; }
        public int? WarehouseId { get; set; }
        public string? WarehouseName { get; set; }
        public string DamageReason { get; set; } = string.Empty;
        public decimal DamagePercent { get; set; }
        public decimal SuggestedDiscountPercent { get; set; }
        public string? Note { get; set; }
        public string EvidenceImageUrl { get; set; } = string.Empty;
        public string ReportedByUserId { get; set; } = string.Empty;
        public string ReportedByUsername { get; set; } = string.Empty;
        public DateTime ReportedAt { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? ReviewedByUserId { get; set; }
        public string? ReviewedByUsername { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public string? ReviewNote { get; set; }
        public decimal? AppliedDiscountPercent { get; set; }
        public string? ProcessingOutcome { get; set; }
        public decimal? ApprovedDamagedWeightKg { get; set; }
        public decimal? BoxWeightSnapshotKg { get; set; }
        public string? RequestedProcessingOutcome { get; set; }
        public decimal? RequestedDamagedWeightKg { get; set; }
        public decimal? BoxWeightAtReportKg { get; set; }
    }
}

