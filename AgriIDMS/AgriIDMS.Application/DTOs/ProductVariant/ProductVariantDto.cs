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
    }

    public class UpdateProductVariantDto
    {
        public ProductGrade? Grade { get; set; }

        [Range(0.01, double.MaxValue, ErrorMessage = "Price phải lớn hơn 0")]
        public decimal? Price { get; set; }

        public bool? IsActive { get; set; }
    }
}
