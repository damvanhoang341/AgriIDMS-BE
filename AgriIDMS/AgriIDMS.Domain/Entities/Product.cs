using AgriIDMS.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgriIDMS.Domain.Entities
{
    public class Product
    {
        //public int Id { get;  set; }          
        //public string Name { get;  set; } = null!;
        //public string? Description { get; set; }
        //public decimal Price { get; set; }
        //public string Unit { get; set; } = null!;
        //public bool IsActive { get; set; }

        //public int CategoryId { get; set; }
        //public Category Category { get; set; } = null!;

        //public ProductStatus Status { get; set; }
        //public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public ICollection<GoodsReceiptDetail> GoodsReceiptDetails { get; set; } = new List<GoodsReceiptDetail>();

        //public ICollection<Lot> Lots { get; set; } = new List<Lot>();
        //public ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
        //public ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();

        public int Id { get; set; }
        public string Name { get; set; } = null!;   // Táo Fuji
        public string? Description { get; set; }

        public int CategoryId { get; set; }
        public Category Category { get; set; } = null!;

        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<ProductVariant> Variants { get; set; } = new List<ProductVariant>();
    }
}
