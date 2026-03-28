using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AgriIDMS.Application.DTOs.Box
{
    public class DisposeExpiredBoxesRequest
    {
        [Required(ErrorMessage = "Danh sách BoxId không được để trống")]
        [MinLength(1, ErrorMessage = "Phải chọn ít nhất 1 box để tiêu hủy")]
        public List<int> BoxIds { get; set; } = new();
    }

    public class DisposeExpiredBoxesResultDto
    {
        public int RequestedCount { get; set; }
        public int DisposedCount { get; set; }
        public int SkippedCount { get; set; }
    }
}
