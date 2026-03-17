using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AgriIDMS.Application.DTOs.Box
{
    /// <summary>Gán nhiều box vào cùng một slot trong một lần gọi.</summary>
    public class AssignBoxesToSlotRequest
    {
        [Required(ErrorMessage = "SlotId không được để trống")]
        public int SlotId { get; set; }

        [Required(ErrorMessage = "Danh sách BoxId không được để trống")]
        [MinLength(1, ErrorMessage = "Cần ít nhất một BoxId")]
        public List<int> BoxIds { get; set; } = new();
    }
}
