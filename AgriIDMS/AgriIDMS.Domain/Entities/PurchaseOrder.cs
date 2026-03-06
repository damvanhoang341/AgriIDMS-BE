using AgriIDMS.Domain.Enums;

namespace AgriIDMS.Domain.Entities
{
    public class PurchaseOrder
    {
        public int Id { get; set; }

        public string OrderCode { get; set; } = null!;

        public int SupplierId { get; set; }
        public Supplier Supplier { get; set; } = null!;

        public PurchaseOrderStatus Status { get; set; } = PurchaseOrderStatus.Pending;

        public DateTime OrderDate { get; set; } = DateTime.UtcNow;
        // === người tạo đơn hàng ===
        public string CreatedBy { get; set; } = null!;
        public ApplicationUser CreatedUser { get; set; } = null!;
        // === người duyệt đơn hàng ===
        public string? ApprovedBy { get; set; }
        public ApplicationUser? ApprovedUser { get; set; }
        public DateTime? ApprovedAt { get; set; }

        public ICollection<PurchaseOrderDetail> Details { get; set; }
            = new List<PurchaseOrderDetail>();
    }
}