namespace AgriIDMS.Application.DTOs.Discount
{
    public enum NearExpiryDiscountSourceType
    {
        None = 0,
        SystemRule = 1,
        ProductOverride = 2
    }

    public class NearExpiryDiscountResultDto
    {
        public decimal AppliedPercent { get; set; }
        public int? RuleId { get; set; }
        public int? OverrideId { get; set; }
        public NearExpiryDiscountSourceType SourceType { get; set; } = NearExpiryDiscountSourceType.None;
        public decimal DiscountAmount { get; set; }
        public decimal FinalUnitPrice { get; set; }
    }
}
