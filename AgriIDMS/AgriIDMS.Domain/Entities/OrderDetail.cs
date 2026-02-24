using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgriIDMS.Domain.Entities
{
    public class OrderDetail
    {
        public int Id { get; set; }

        public int OrderId { get; set; }
        public Order Order { get; set; } = null!;

        public int ProductId { get; set; }
        public Product Product { get; set; } 

        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal FulfilledQuantity { get; set; }
        public decimal ShortageQuantity { get; set; }

        public ICollection<OrderAllocation> Allocations { get; set; }
            = new List<OrderAllocation>();
    }
}
