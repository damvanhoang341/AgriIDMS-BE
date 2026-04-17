using System;

namespace AgriIDMS.Application.DTOs.Lot
{
    public class NearExpiryDiscountRuleDto
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public int? MinDaysLeft { get; set; }
        public int MaxDaysLeft { get; set; }
        public decimal DiscountPercent { get; set; }
        public int Priority { get; set; }
        public bool IsActive { get; set; }
        public DateTime? StartAtUtc { get; set; }
        public DateTime? EndAtUtc { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class UpsertNearExpiryDiscountRuleDto
    {
        public string? Name { get; set; }
        public int? MinDaysLeft { get; set; }
        public int MaxDaysLeft { get; set; }
        public decimal DiscountPercent { get; set; }
        public int Priority { get; set; } = 1;
        public bool IsActive { get; set; } = true;
        public DateTime? StartAtUtc { get; set; }
        public DateTime? EndAtUtc { get; set; }
    }
}

