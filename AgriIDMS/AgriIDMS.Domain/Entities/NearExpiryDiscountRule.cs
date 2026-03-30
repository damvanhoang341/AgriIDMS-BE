using System;

namespace AgriIDMS.Domain.Entities
{
    public class NearExpiryDiscountRule
    {
        public int Id { get; set; }

        /// <summary>
        /// Áp dụng khi số ngày còn lại <= MaxDaysLeft.
        /// </summary>
        public int MaxDaysLeft { get; set; }

        /// <summary>
        /// % giảm giá đề xuất (0-100).
        /// </summary>
        public decimal DiscountPercent { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? CreatedBy { get; set; }

        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }
    }
}

