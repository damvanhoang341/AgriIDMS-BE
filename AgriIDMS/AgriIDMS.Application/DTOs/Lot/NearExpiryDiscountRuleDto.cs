using System;

namespace AgriIDMS.Application.DTOs.Lot
{
    public class NearExpiryDiscountRuleDto
    {
        public int Id { get; set; }
        public int MaxDaysLeft { get; set; }
        public decimal DiscountPercent { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class UpsertNearExpiryDiscountRuleDto
    {
        public int MaxDaysLeft { get; set; }
        public decimal DiscountPercent { get; set; }
        public bool IsActive { get; set; } = true;
    }
}

