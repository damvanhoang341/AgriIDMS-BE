using System;
using System.ComponentModel.DataAnnotations;

namespace AgriIDMS.Application.DTOs.Review
{
    public class CreateReviewRequest
    {
        [Range(1, int.MaxValue)]
        public int OrderDetailId { get; set; }

        [Range(1, 5)]
        public int Rating { get; set; }

        [Range(1, 5)]
        public int Freshness { get; set; }

        [Range(1, 5)]
        public int Packaging { get; set; }

        [MaxLength(1000)]
        public string? Comment { get; set; }
    }

    public class ReviewResponseDto
    {
        public int Id { get; set; }
        public int OrderDetailId { get; set; }
        public int ProductVariantId { get; set; }
        public string CustomerId { get; set; } = string.Empty;
        public int Rating { get; set; }
        public int Freshness { get; set; }
        public int Packaging { get; set; }
        public string? Comment { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
