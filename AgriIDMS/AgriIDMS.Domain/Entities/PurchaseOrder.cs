using AgriIDMS.Domain.Enums;
using System.Collections.Generic;
using System.Linq;

namespace AgriIDMS.Domain.Entities
{
    public class PurchaseOrder
    {
        public int Id { get; set; }
        public string OrderCode { get; set; } = null!;

        public int SupplierId { get; set; }
        public Supplier Supplier { get; set; } = null!;

        public PurchaseOrderStatus Status { get; set; } = PurchaseOrderStatus.Pending;
        public DateTime OrderDate { get; set; } = System.DateTime.UtcNow;

        public string CreatedBy { get; set; } = null!;
        public ApplicationUser CreatedUser { get; set; } = null!;
        public string? ApprovedBy { get; set; }
        public ApplicationUser? ApprovedUser { get; set; }
        public System.DateTime? ApprovedAt { get; set; }

        public decimal TotalAmount => Details.Sum(x => x.OrderedWeight * x.UnitPrice);

        public ICollection<PurchaseOrderDetail> Details { get; set; } = new List<PurchaseOrderDetail>();
        public ICollection<GoodsReceipt> GoodsReceipts { get; set; } = new List<GoodsReceipt>();
    }
}
