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
        public string Name { get; set; } = null!;
        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;
        public ProductGrade Grade { get; set; } 
        public decimal Price { get; set; }

        /// <summary>Số ngày bảo quản (shelf life) kể từ ngày thu hoạch để tính hạn sử dụng.</summary>
        public int ShelfLifeDays { get; set; }

        /// <summary>Đường dẫn ảnh đại diện cho biến thể sản phẩm.</summary>
        public string? ImageUrl { get; set; }
        /// <summary>Định mức tối thiểu (kg) cho mỗi dòng nhập sản phẩm này. Null = không bắt buộc.</summary>
        public decimal? MinReceiptWeight { get; set; }

        public bool IsActive { get; set; } = true;

        /// <summary>
        /// % giảm giá khi tồn đủ điều kiện gần hết hạn (theo Pricing:NearExpiryDiscountDays).
        /// Null = dùng Pricing:NearExpiryDiscountPercent trong cấu hình.
        /// </summary>
        public decimal? ManualNearExpiryDiscountPercent { get; set; }

        public ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
        public ICollection<GoodsReceiptDetail> GoodsReceiptDetails { get; set; } = new List<GoodsReceiptDetail>();
        public ICollection<Review> Reviews { get; set; } = new List<Review>();
    }
}
