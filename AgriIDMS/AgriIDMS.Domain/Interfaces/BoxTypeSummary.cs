using System;

namespace AgriIDMS.Domain.Interfaces
{
    /// <summary>Tổng hợp số lượng box theo loại (full/partial) và trọng lượng.</summary>
    public class BoxTypeSummary
    {
        public bool IsPartial { get; set; }
        public decimal Weight { get; set; }
        public int AvailableCount { get; set; }
    }
}

