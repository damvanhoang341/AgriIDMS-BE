using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgriIDMS.Application.DTOs.Category
{
    public class CreateCategoryRequest
    {
        [Required(ErrorMessage = "Tên danh mục không được để trống.")]
        [StringLength(100, ErrorMessage = "Tên danh mục tối đa 100 ký tự.")]
        public string Name { get; set; } = null!;

        [StringLength(500, ErrorMessage = "Mô tả tối đa 500 ký tự.")]
        public string? Description { get; set; }
    }

    public class UpdateCategoryRequest
    {
        [StringLength(100, ErrorMessage = "Tên danh mục tối đa 100 ký tự.")]
        public string? Name { get; set; }

        [StringLength(500, ErrorMessage = "Mô tả tối đa 500 ký tự.")]
        public string? Description { get; set; }

        public int? Status { get; set; }
    }

    public class UpdateStatusCategoryRequest
    {
        [Required(ErrorMessage = "Chuyển trạng thái không được để trống")]
        public int Status { get; set; }
    }

    public class CategoryDto
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }
    }
}
