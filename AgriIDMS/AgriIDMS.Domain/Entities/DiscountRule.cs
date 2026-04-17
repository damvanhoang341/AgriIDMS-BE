using AgriIDMS.Domain.Enums;
using System;

namespace AgriIDMS.Domain.Entities
{
    public class DiscountRule
    {
        public int Id { get; set; }

        /// <summary>
        /// Loại rule giảm giá (runtime hiện dùng NearExpiry).
        /// </summary>
        public DiscountRuleType RuleType { get; set; } = DiscountRuleType.NearExpiry;

        /// <summary>
        /// Tên rule để dễ quản trị. Có thể null với dữ liệu legacy.
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Áp dụng khi số ngày còn lại &lt;= MaxDaysLeft (chỉ dùng cho NearExpiry).
        /// </summary>
        public int? MaxDaysLeft { get; set; }

        /// <summary>
        /// % giảm giá (0-100).
        /// </summary>
        public decimal DiscountPercent { get; set; }

        /// <summary>
        /// Số càng nhỏ càng được ưu tiên áp dụng trước.
        /// </summary>
        public int Priority { get; set; } = 100;

        /// <summary>
        /// Trường điều kiện mở rộng legacy.
        /// Giữ lại để tương thích dữ liệu cũ, hiện không dùng trong runtime giảm giá.
        /// </summary>
        public string? ConditionsJson { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? CreatedBy { get; set; }

        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }
    }
}

