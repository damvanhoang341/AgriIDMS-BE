using System;

namespace AgriIDMS.Application.DTOs.StockCheck
{
    public class StockCheckDashboardItemDto
    {
        public int StockCheckId { get; set; }
        public string Status { get; set; } = string.Empty;
        public string CheckType { get; set; } = string.Empty;
        public DateTime SnapshotAt { get; set; }
        public DateTime CreatedAt { get; set; }

        public int TotalLines { get; set; }
        public int CountedLines { get; set; }

        public int ShortageLines { get; set; }
        public int ExcessLines { get; set; }
    }
}

