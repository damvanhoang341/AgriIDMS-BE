using AgriIDMS.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgriIDMS.Domain.Entities
{
    public class InventoryTransaction
    {
        public int Id { get; set; }

        public int BoxId { get; set; }
        public Box Box { get; set; } = null!;

        public InventoryTransactionType TransactionType { get; set; }

        public int? FromSlotId { get; set; }
        public int? ToSlotId { get; set; }

        public decimal Quantity { get; set; }

        public ReferenceType? ReferenceType { get; set; }
        public int? ReferenceRequestId { get; set; }

        public string CreatedBy { get; set; }
        public ApplicationUser CreatedUser { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public int? InventoryRequestId { get; set; }
        public InventoryRequest? InventoryRequest { get; set; }
    }
}
