using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AgriIDMS.Application.DTOs.Export
{
    public class CreateExportReceiptRequest
    {
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "OrderId phải lớn hơn 0")]
        public int OrderId { get; set; }
    }

    public class ExportReceiptResponseDto
    {
        public int Id { get; set; }
        public string ExportCode { get; set; } = null!;
        public int OrderId { get; set; }
        public string Status { get; set; } = null!;
        public string CreatedBy { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public List<ExportDetailDto> Details { get; set; } = new();
    }

    public class ExportDetailDto
    {
        public int Id { get; set; }
        public int BoxId { get; set; }
        public string BoxCode { get; set; } = null!;
        public decimal ActualQuantity { get; set; }
        public string BoxStatus { get; set; } = null!;
    }
}
