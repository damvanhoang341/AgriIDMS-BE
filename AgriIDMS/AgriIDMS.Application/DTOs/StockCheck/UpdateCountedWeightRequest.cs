using System.ComponentModel.DataAnnotations;
using AgriIDMS.Domain.Enums;

namespace AgriIDMS.Application.DTOs.StockCheck
{
    public class UpdateCountedWeightRequest
    {
        [Required]
        public int StockCheckDetailId { get; set; }

        [Required]
        [Range(0, double.MaxValue, ErrorMessage = "CountedWeight phải >= 0")]
        public decimal CountedWeight { get; set; }

        [MaxLength(500)]
        public string? Note { get; set; }

        /// <summary>
        /// Nguyên nhân chênh lệch (bắt buộc khi thiếu hàng / Shortage).
        /// </summary>
        public VarianceReason? VarianceReason { get; set; }
    }
}
