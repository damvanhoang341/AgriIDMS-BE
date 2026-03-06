namespace AgriIDMS.Domain.Entities
{
    public class PurchaseOrderDetail
    {
        public int Id { get; set; }

        public int PurchaseOrderId { get; set; }
        public PurchaseOrder PurchaseOrder { get; set; } = null!;

        public int ProductVariantId { get; set; }
        public ProductVariant ProductVariant { get; set; } = null!;

        public decimal OrderedWeight { get; set; }

        public decimal UnitPrice { get; set; }

        public ICollection<GoodsReceiptDetail> GoodsReceiptDetails { get; set; }
            = new List<GoodsReceiptDetail>();
    }
}