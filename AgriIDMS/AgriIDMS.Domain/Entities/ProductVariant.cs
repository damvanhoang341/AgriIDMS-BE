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
        /// <summary>Khối lượng riêng sản phẩm (kg/m3).</summary>
        public decimal DensityKgPerM3 { get; set; }

        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Deprecated: giữ tạm để hỗ trợ dữ liệu cũ.
        /// Luồng mới dùng ProductVariantDiscountOverrides.
        /// </summary>
        public decimal? ManualNearExpiryDiscountPercent { get; set; }

        public ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();
        public ICollection<GoodsReceiptDetail> GoodsReceiptDetails { get; set; } = new List<GoodsReceiptDetail>();
        public ICollection<Lot> Lots { get; set; } = new List<Lot>();
        public ICollection<QcClassificationDetail> QcClassificationDetails { get; set; } = new List<QcClassificationDetail>();
        public ICollection<Review> Reviews { get; set; } = new List<Review>();
        public ICollection<ProductVariantDiscountOverride> DiscountOverrides { get; set; } = new List<ProductVariantDiscountOverride>();
    }
}
