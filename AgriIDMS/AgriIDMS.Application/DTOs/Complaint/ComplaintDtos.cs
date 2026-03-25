using AgriIDMS.Domain.Enums;
using System;
using System.ComponentModel.DataAnnotations;

namespace AgriIDMS.Application.DTOs.Complaint
{
    public class CreateComplaintRequest
    {
        [Required]
        [Range(1, int.MaxValue)]
        public int OrderId { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        public int BoxId { get; set; }

        [Required]
        public ComplaintType Type { get; set; }

        [Range(0.0001, double.MaxValue, ErrorMessage = "DamagedQuantity phải lớn hơn 0")]
        public decimal DamagedQuantity { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        [MaxLength(500)]
        public string? CustomerEvidenceUrl { get; set; }
    }

    public class VerifyComplaintRequest
    {
        /// <summary>true = chấp nhận khiếu nại (Verified), false = từ chối (Rejected).</summary>
        [Required]
        public bool Approved { get; set; }
    }

    public class ComplaintResponseDto
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public int BoxId { get; set; }
        public string Type { get; set; } = null!;
        public string Status { get; set; } = null!;
        public decimal DamagedQuantity { get; set; }
        public string? Description { get; set; }
        public string? CustomerEvidenceUrl { get; set; }
        public bool IsVerified { get; set; }
        public string? VerifiedBy { get; set; }
        public DateTime? VerifiedAt { get; set; }
        public DateTime CreatedAt { get; set; }

        /// <summary>Mã box (hiển thị nhanh).</summary>
        public string? BoxCode { get; set; }
    }

    public class ComplaintableBoxListItemDto
    {
        public int BoxId { get; set; }
        public string BoxCode { get; set; } = null!;
        public decimal ReservedQuantity { get; set; }
        /// <summary>
        /// Số lượng tối đa FE có thể cho khiếu nại (match logic ValidateDamagedQuantity trong backend).
        /// </summary>
        public decimal ComplaintableQuantity { get; set; }
        public bool HasPendingComplaint { get; set; }
    }

    public class EligibleOrderForComplaintListItemDto
    {
        public int OrderId { get; set; }
        public string Status { get; set; } = null!;
        public int BoxCount { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
