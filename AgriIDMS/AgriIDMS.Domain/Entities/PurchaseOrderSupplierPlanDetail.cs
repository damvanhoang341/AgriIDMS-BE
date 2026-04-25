namespace AgriIDMS.Domain.Entities
{
    public class PurchaseOrderSupplierPlanDetail
    {
        public int Id { get; set; }

        public int SupplierPlanId { get; set; }
        public PurchaseOrderSupplierPlan SupplierPlan { get; set; } = null!;

        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;

        public decimal OrderedWeight { get; set; }
        public decimal UnitPriceAtOrder { get; set; }
        public DateTime PriceDate { get; set; }
        public decimal TolerancePercent { get; set; } = 0;

        public ICollection<PurchaseOrderDetail> PurchaseOrderDetails { get; set; } = new List<PurchaseOrderDetail>();
        public ICollection<GoodsReceiptDetail> GoodsReceiptDetails { get; set; } = new List<GoodsReceiptDetail>();
    }
}
