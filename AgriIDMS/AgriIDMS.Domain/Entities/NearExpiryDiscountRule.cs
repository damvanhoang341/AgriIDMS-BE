using AgriIDMS.Domain.Enums;
using System;

namespace AgriIDMS.Domain.Entities
{
    public class DiscountRule
    {
        public int Id { get; set; }

        /// <summary>
        /// NearExpiry: giảm theo số ngày còn hạn.
        /// FreeStyle: giảm linh hoạt theo campaign.
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
        /// JSON điều kiện mở rộng cho FreeStyle (optional).
        /// </summary>
        public string? ConditionsJson { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string? CreatedBy { get; set; }

        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }
    }
}

