using AgriIDMS.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgriIDMS.Domain.Entities
{
    public class OrderAllocation
    {
        public int Id { get; set; }

        public int OrderId { get; set; }
        public Order Order { get; set; } = null!;

        public int OrderDetailId { get; set; }
        public OrderDetail OrderDetail { get; set; } = null!;

        public int BoxId { get; set; }
        public Box Box { get; set; } = null!;

        public decimal ReservedQuantity { get; set; }

        public decimal? PickedQuantity { get; set; }

        public AllocationStatus Status { get; set; } = AllocationStatus.Reserved;

        public DateTime ReservedAt { get; set; } = DateTime.UtcNow;

        public DateTime? ExpiredAt { get; set; }
    }
}
