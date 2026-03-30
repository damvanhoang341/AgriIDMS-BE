using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace AgriIDMS.Application.DTOs.Disposal
{
    public class CreateDisposalRequestDto
    {
        [Required(ErrorMessage = "WarehouseId không được để trống")]
        public int WarehouseId { get; set; }

        [Required(ErrorMessage = "Danh sách BoxId không được để trống")]
        [MinLength(1, ErrorMessage = "Phải chọn ít nhất 1 box để tiêu hủy")]
        public List<int> BoxIds { get; set; } = new();

        [Required(ErrorMessage = "Lý do tiêu hủy không được để trống")]
        [MaxLength(1000)]
        public string Reason { get; set; } = string.Empty;
    }

    public class DisposalRequestListItemDto
    {
        public int Id { get; set; }
        public string Status { get; set; } = string.Empty;
        public int WarehouseId { get; set; }
        public string WarehouseName { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public string RequestedBy { get; set; } = string.Empty;
        public string? RequestedByName { get; set; }
        public DateTime RequestedAt { get; set; }
        public string? ReviewedBy { get; set; }
        public string? ReviewedByName { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public string? ReviewNote { get; set; }
        public int BoxCount { get; set; }
    }

    public class DisposalRequestDetailItemDto
    {
        public int BoxId { get; set; }
        public string BoxCode { get; set; } = string.Empty;
        public decimal Weight { get; set; }
        public string? LotCode { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public string? SlotCode { get; set; }
        public string? ProductName { get; set; }
        public string? ProductVariantName { get; set; }
    }

    public class DisposalRequestDetailDto : DisposalRequestListItemDto
    {
        public List<DisposalRequestDetailItemDto> Items { get; set; } = new();
    }
}

