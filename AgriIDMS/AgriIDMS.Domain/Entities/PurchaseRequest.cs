using AgriIDMS.Domain.Enums;

namespace AgriIDMS.Domain.Entities
{
    public class PurchaseRequest
    {
        public int Id { get; set; }
        public string RequestCode { get; set; } = null!;
        public PurchaseRequestStatus Status { get; set; } = PurchaseRequestStatus.Draft;
        public DateTime RequestedDate { get; set; } = DateTime.UtcNow;
        public string CreatedBy { get; set; } = null!;
        public ApplicationUser CreatedUser { get; set; } = null!;
        public string? Notes { get; set; }

        public ICollection<PurchaseRequestDetail> Details { get; set; } = new List<PurchaseRequestDetail>();
    }
}
