using AgriIDMS.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgriIDMS.Domain.Entities
{
    public class StockCheck
    {
        public int Id { get; set; }

        public int WarehouseId { get; set; }
        public Warehouse Warehouse { get; set; }

        public StockCheckType CheckType { get; set; } // Full / Cycle / Spot

        public StockCheckStatus Status { get; set; } // Draft / InProgress / Counted / Approved
        public DateTime SnapshotAt { get; set; }
        public bool IsLockedSnapshot { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; }
        public ApplicationUser CreatedUser { get; set; }

        public string? ApprovedBy { get; set; }
        public ApplicationUser? ApprovedUser { get; set; }
        public DateTime? ApprovedAt { get; set; }

        public ICollection<StockCheckDetail> Details { get; set; }
    }
}
