using AgriIDMS.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgriIDMS.Domain.Entities
{
    public class InventoryRequest
    {
        public int Id { get; set; }

        public InventoryRequestType RequestType { get; set; }

        public InventoryReferenceType? ReferenceType { get; set; }

        public int? ReferenceId { get; set; }

        public string? OldValue { get; set; }

        public string? NewValue { get; set; }

        public string? Reason { get; set; }

        public string CreatedBy { get; set; } = null!;

        public DateTime CreatedAt { get; set; }

        public InventoryRequestStatus Status { get; set; }
            = InventoryRequestStatus.Pending;

        public string? ApprovedBy { get; set; }

        public DateTime? ApprovedAt { get; set; }

        // Navigation
        public ApplicationUser CreatedUser { get; set; } = null!;
        public ApplicationUser? ApprovedUser { get; set; }

        public ICollection<InventoryTransaction> Transactions { get; set; }
            = new List<InventoryTransaction>();
    }
}
