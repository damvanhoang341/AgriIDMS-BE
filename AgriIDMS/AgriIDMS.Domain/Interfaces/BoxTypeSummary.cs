namespace AgriIDMS.Domain.Interfaces
{
    /// <summary>Tổng hợp số lượng box theo đầy/lẻ và trọng lượng (không phải loại bao bì).</summary>
    public class BoxTypeSummary
    {
        public bool IsPartial { get; set; }
        public decimal Weight { get; set; }
        public int AvailableCount { get; set; }
    }
}

