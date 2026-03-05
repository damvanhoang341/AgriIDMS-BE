using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgriIDMS.Application.DTOs.Product
{
    public class CreateProductRequest
    {
        [Required(ErrorMessage = "Tên sản phẩm không được để trống")]
        [StringLength(150, ErrorMessage = "Tên sản phẩm tối đa 150 ký tự")]
        public string Name { get; set; } = null!;

        [StringLength(500, ErrorMessage = "Mô tả tối đa 500 ký tự")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "CategoryId không được để trống")]
        [Range(1, int.MaxValue, ErrorMessage = "CategoryId không hợp lệ")]
        public int CategoryId { get; set; }
    }

    public class UpdateProductRequest
    {
        [StringLength(150, ErrorMessage = "Tên sản phẩm tối đa 150 ký tự")]
        public string? Name { get; set; }

        [StringLength(500, ErrorMessage = "Mô tả tối đa 500 ký tự")]
        public string? Description { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "CategoryId không hợp lệ")]
        public int? CategoryId { get; set; }

        public bool? IsActive { get; set; }
    }
}
