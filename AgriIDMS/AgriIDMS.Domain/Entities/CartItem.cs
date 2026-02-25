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

        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; } // snapshot giá tại thời điểm thêm
    }
}
