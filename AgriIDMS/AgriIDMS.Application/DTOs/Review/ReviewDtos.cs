using System;
using System.Collections.Generic;
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

    public class GetApprovedReviewsQuery
    {
        [Range(0, int.MaxValue)]
        public int Skip { get; set; } = 0;

        [Range(1, 100)]
        public int Take { get; set; } = 10;
    }

    public class ApprovedReviewItemDto
    {
        public int Id { get; set; }
        public int ProductVariantId { get; set; }
        public string CustomerId { get; set; } = string.Empty;
        public string? CustomerName { get; set; }
        public int Rating { get; set; }
        public int Freshness { get; set; }
        public int Packaging { get; set; }
        public string? Comment { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class ApprovedReviewListResponseDto
    {
        public int ProductVariantId { get; set; }
        public int Skip { get; set; }
        public int Take { get; set; }
        public int Count { get; set; }
        public IList<ApprovedReviewItemDto> Items { get; set; } = new List<ApprovedReviewItemDto>();
    }
}
