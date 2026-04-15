using System;

namespace AgriIDMS.Application.DTOs.Lot
{
    public class ProductVariantDiscountOverrideDto
    {
        public int Id { get; set; }
        public int ProductVariantId { get; set; }
        public decimal OverrideNearExpiryDiscountPercent { get; set; }
        public string? Reason { get; set; }
        public bool IsActive { get; set; }
        public DateTime? StartAtUtc { get; set; }
        public DateTime? EndAtUtc { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class UpsertProductVariantDiscountOverrideDto
    {
        public int ProductVariantId { get; set; }
        public decimal OverrideNearExpiryDiscountPercent { get; set; }
        public string? Reason { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime? StartAtUtc { get; set; }
        public DateTime? EndAtUtc { get; set; }
    }
}
