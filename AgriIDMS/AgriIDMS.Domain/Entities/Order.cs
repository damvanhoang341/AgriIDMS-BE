using AgriIDMS.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgriIDMS.Domain.Entities
{
    public class Order
    {
        public int Id { get; set; }

        public string UserId { get; set; }

        public DateTime CreatedAt { get; set; }

        public decimal TotalAmount { get; set; }

        public OrderStatus Status { get; set; }

        public ICollection<OrderDetail> Details { get; set; } = new List<OrderDetail>();
        public ICollection<Payment> Payments { get; set; } = new List<Payment>();
        public ICollection<OrderAllocation> Allocations { get; set; } = new List<OrderAllocation>();
    }
}
