using System;

namespace AgriIDMS.Domain.Entities
{
    public class ProductVariantDiscountOverride
    {
        public int Id { get; set; }
        public int ProductVariantId { get; set; }
        public ProductVariant ProductVariant { get; set; } = null!;
        public decimal OverrideNearExpiryDiscountPercent { get; set; }
        public int Priority { get; set; }
        public string? Reason { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime? StartAtUtc { get; set; }
        public DateTime? EndAtUtc { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }
    }
}
