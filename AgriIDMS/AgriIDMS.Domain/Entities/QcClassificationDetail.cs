namespace AgriIDMS.Domain.Entities
{
    public class QcClassificationDetail
    {
        public int Id { get; set; }

        public int QcRecordId { get; set; }
        public QcRecord QcRecord { get; set; } = null!;

        public int ProductVariantId { get; set; }
        public ProductVariant ProductVariant { get; set; } = null!;

        public decimal Quantity { get; set; }
    }
}
