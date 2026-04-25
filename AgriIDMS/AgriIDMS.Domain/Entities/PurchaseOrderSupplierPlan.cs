namespace AgriIDMS.Domain.Entities
{
    public class PurchaseOrderSupplierPlan
    {
        public int Id { get; set; }

        public int PurchaseOrderId { get; set; }
        public PurchaseOrder PurchaseOrder { get; set; } = null!;

        public int SupplierId { get; set; }
        public Supplier Supplier { get; set; } = null!;

        public DateTime OrderDate { get; set; }
        public string? Notes { get; set; }

        public ICollection<PurchaseOrderSupplierPlanDetail> Details { get; set; } = new List<PurchaseOrderSupplierPlanDetail>();
    }
}
