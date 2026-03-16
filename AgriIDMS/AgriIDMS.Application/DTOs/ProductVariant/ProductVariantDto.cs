using AgriIDMS.Domain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgriIDMS.Application.DTOs.ProductVariant
{
    public class ProductVariantResponseDto
    {
        public int Id { get; set; }

        public int ProductId { get; set; }

        public string ProductName { get; set; } = null!;

        public ProductGrade Grade { get; set; }

        public decimal Price { get; set; }

        public bool IsActive { get; set; }

        /// <summary>Số ngày bảo quản (shelf life) kể từ ngày thu hoạch.</summary>
        public int ShelfLifeDays { get; set; }

        /// <summary>Đường dẫn ảnh của biến thể.</summary>
        public string? ImageUrl { get; set; }

        /// <summary>Định mức tối thiểu (kg) cho mỗi dòng nhập sản phẩm này. Null = không bắt buộc.</summary>
        public decimal? MinReceiptWeight { get; set; }

        /// <summary>Số box khả dụng trong kho.</summary>
        public int AvailableBoxCount { get; set; }
    }

    public class ProductVariantResponseCustomerDto
    {
        public int Id { get; set; }

        public int ProductId { get; set; }

        public string ProductName { get; set; } = null!;

        public ProductGrade Grade { get; set; }

        public decimal Price { get; set; }

        public bool IsActive { get; set; }

        /// <summary>Số ngày bảo quản (shelf life) kể từ ngày thu hoạch.</summary>
        public int ShelfLifeDays { get; set; }

        /// <summary>Đường dẫn ảnh của biến thể.</summary>
        public string? ImageUrl { get; set; }

        /// <summary>Số box khả dụng trong kho.</summary>
        public int AvailableBoxCount { get; set; }
    }

    public class CreateProductVariantDto
    {
        [Required(ErrorMessage = "ProductId không được để trống")]
        [Range(1, int.MaxValue, ErrorMessage = "ProductId không hợp lệ")]
        public int ProductId { get; set; }

        [Required(ErrorMessage = "Grade không được để trống")]
        public ProductGrade Grade { get; set; }

        [Required(ErrorMessage = "Price không được để trống")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Price phải lớn hơn 0")]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "ShelfLifeDays không được để trống")]
        [Range(0, int.MaxValue, ErrorMessage = "ShelfLifeDays phải >= 0")]
        public int ShelfLifeDays { get; set; }

        [StringLength(500, ErrorMessage = "ImageUrl tối đa 500 ký tự")]
        public string? ImageUrl { get; set; }

        /// <summary>Định mức tối thiểu (kg) cho mỗi dòng nhập. Null = không bắt buộc.</summary>
        [Range(0, double.MaxValue, ErrorMessage = "MinReceiptWeight phải >= 0")]
        public decimal? MinReceiptWeight { get; set; }
    }

    public class UpdateProductVariantDto
    {
        public ProductGrade? Grade { get; set; }

        [Range(0.01, double.MaxValue, ErrorMessage = "Price phải lớn hơn 0")]
        public decimal? Price { get; set; }

        public bool? IsActive { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "ShelfLifeDays phải >= 0")]
        public int? ShelfLifeDays { get; set; }

        [StringLength(500, ErrorMessage = "ImageUrl tối đa 500 ký tự")]
        public string? ImageUrl { get; set; }

        /// <summary>Định mức tối thiểu (kg) cho mỗi dòng nhập. Null = không bắt buộc.</summary>
        [Range(0, double.MaxValue, ErrorMessage = "MinReceiptWeight phải >= 0")]
        public decimal? MinReceiptWeight { get; set; }
    }

    public class UpdateProductVariantStatusDto
    {
        [Required(ErrorMessage = "Chuyển trạng thái không được để trống")]
        public bool IsActive { get; set; }
    }
}
