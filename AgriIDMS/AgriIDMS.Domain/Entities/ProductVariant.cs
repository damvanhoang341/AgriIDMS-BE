using AgriIDMS.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgriIDMS.Domain.Entities
{
    public class ProductVariant
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;
        public ProductGrade Grade { get; set; } 
        public decimal Price { get; set; }

        /// <summary>Số ngày bảo quản (shelf life) kể từ ngày thu hoạch để tính hạn sử dụng.</summary>
        public int ShelfLifeDays { get; set; }

        /// <summary>Đường dẫn ảnh đại diện cho biến thể sản phẩm.</summary>
        public string? ImageUrl { get; set; }

        public bool IsActive { get; set; } = true;

        public ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
        public ICollection<GoodsReceiptDetail> GoodsReceiptDetails { get; set; } = new List<GoodsReceiptDetail>();
        public ICollection<Review> Reviews { get; set; } = new List<Review>();
    }
}
