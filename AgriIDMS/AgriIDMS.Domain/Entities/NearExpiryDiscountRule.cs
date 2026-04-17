using System;

namespace AgriIDMS.Domain.Entities
{
    public class NearExpiryDiscountRule
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int? MinDaysLeft { get; set; }
        public int MaxDaysLeft { get; set; }
        public decimal DiscountPercent { get; set; }
        public int Priority { get; set; } = 100;
        public bool IsActive { get; set; } = true;
        public DateTime? StartAtUtc { get; set; }
        public DateTime? EndAtUtc { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }
    }
}
