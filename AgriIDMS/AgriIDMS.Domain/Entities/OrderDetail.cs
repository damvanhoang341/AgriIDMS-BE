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

        public int ProductVariantId { get; set; }
        public ProductVariant ProductVariant { get; set; } = null!;

        /// <summary>Trọng lượng mỗi box (kg) theo loại box khách chọn.</summary>
        public decimal BoxWeight { get; set; }
        /// <summary>Box lẻ (partial) hay box đầy theo loại box khách chọn.</summary>
        public bool IsPartial { get; set; }

        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal FulfilledQuantity { get; set; }
        public decimal ShortageQuantity { get; set; }
        public Review? Review { get; set; }

        public ICollection<OrderAllocation> Allocations { get; set; }
            = new List<OrderAllocation>();
    }
}
