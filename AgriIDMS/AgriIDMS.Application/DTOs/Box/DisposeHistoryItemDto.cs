using System;

namespace AgriIDMS.Application.DTOs.Box
{
    public class DisposeHistoryItemDto
    {
        public int TransactionId { get; set; }
        public int BoxId { get; set; }
        public string BoxCode { get; set; } = string.Empty;
        public int? LotId { get; set; }
        public string? LotCode { get; set; }
        public string? ProductName { get; set; }
        public string? ProductVariantName { get; set; }
        public decimal Quantity { get; set; }
        public int? FromSlotId { get; set; }
        public string? FromSlotCode { get; set; }
        public int? WarehouseId { get; set; }
        public string? WarehouseName { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public string? CreatedByName { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
