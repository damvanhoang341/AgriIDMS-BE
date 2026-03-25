using System;

namespace AgriIDMS.Application.DTOs.StockCheck
{
    public class StockCheckDetailDto
    {
        public int StockCheckDetailId { get; set; }
        public int BoxId { get; set; }
        public string BoxCode { get; set; } = string.Empty;

        public string LotCode { get; set; } = string.Empty;
        public string? SlotCode { get; set; }

        public decimal SnapshotWeight { get; set; }
        public decimal? CurrentSystemWeight { get; set; }

        public decimal? CountedWeight { get; set; }
        public decimal? DifferenceWeight { get; set; }

        public string? VarianceType { get; set; }
        public string? VarianceReason { get; set; }

        public string? CountedBy { get; set; }
        public DateTime? CountedAt { get; set; }
        public string? Note { get; set; }
    }
}

