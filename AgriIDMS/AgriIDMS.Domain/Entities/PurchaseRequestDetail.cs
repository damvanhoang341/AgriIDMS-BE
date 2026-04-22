namespace AgriIDMS.Domain.Entities
{
    public class PurchaseRequestDetail
    {
        public int Id { get; set; }

        public int PurchaseRequestId { get; set; }
        public PurchaseRequest PurchaseRequest { get; set; } = null!;

        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;

        public decimal RequestedWeight { get; set; }
        public decimal AllocatedWeight { get; set; }
        public decimal TargetUnitPrice { get; set; }

        public decimal RemainingWeight => RequestedWeight - AllocatedWeight;
    }
}
