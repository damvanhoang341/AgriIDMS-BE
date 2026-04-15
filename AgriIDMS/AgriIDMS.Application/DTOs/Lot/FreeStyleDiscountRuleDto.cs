using System;
using System.Collections.Generic;

namespace AgriIDMS.Application.DTOs.Lot
{
    public class FreeStyleDiscountConditionsDto
    {
        public List<string> Channels { get; set; } = new();
        public bool IsGuestAllowed { get; set; } = false;
        public decimal? MinSubtotal { get; set; }
        public List<int> ProductVariantIds { get; set; } = new();
    }

    public class FreeStyleDiscountRuleDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal DiscountPercent { get; set; }
        public int Priority { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public FreeStyleDiscountConditionsDto Conditions { get; set; } = new();
    }

    public class UpsertFreeStyleDiscountRuleDto
    {
        public string Name { get; set; } = string.Empty;
        public decimal DiscountPercent { get; set; }
        public int Priority { get; set; } = 100;
        public bool IsActive { get; set; } = true;
        public FreeStyleDiscountConditionsDto Conditions { get; set; } = new();
    }

    public class FreeStyleDiscountPreviewRequestDto
    {
        public string Channel { get; set; } = "Online";
        public bool IsGuest { get; set; }
        public decimal Subtotal { get; set; }
        public List<int> ProductVariantIds { get; set; } = new();
    }

    public class FreeStyleDiscountPreviewResponseDto
    {
        public bool Matched { get; set; }
        public int? AppliedRuleId { get; set; }
        public string? AppliedRuleName { get; set; }
        public decimal DiscountPercent { get; set; }
        public decimal SubtotalBefore { get; set; }
        public decimal SubtotalAfter { get; set; }
        public string Reason { get; set; } = string.Empty;
    }
}
