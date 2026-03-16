using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgriIDMS.Domain.Entities
{
    public class CartItem
    {
        public int Id { get; set; }

        public int CartId { get; set; }
        public Cart Cart { get; set; } = null!;

        public int ProductVariantId { get; set; }
        public ProductVariant ProductVariant { get; set; }

        /// <summary>Số box của loại box này.</summary>
        public decimal Quantity { get; set; }
        /// <summary>Trọng lượng mỗi box (kg).</summary>
        public decimal BoxWeight { get; set; }
        /// <summary>Box lẻ (partial) hay box đầy.</summary>
        public bool IsPartial { get; set; }
        /// <summary>Đơn giá theo kg tại thời điểm thêm.</summary>
        public decimal UnitPrice { get; set; } // snapshot giá tại thời điểm thêm
    }
}
