using System;
using System.Collections.Generic;

namespace AgriIDMS.Application.DTOs.Warehouse
{
    public class SlotBoxItemDto
    {
        public int Id { get; set; }
        public string BoxCode { get; set; } = null!;
        public string? QrCode { get; set; }
        public decimal Weight { get; set; }
        public string Status { get; set; } = null!;

        public int LotId { get; set; }
        public string LotCode { get; set; } = null!;
        public DateTime ReceivedDate { get; set; }
        public DateTime ExpiryDate { get; set; }
    }

    public class SlotContentsDto
    {
        public int SlotId { get; set; }
        public string SlotCode { get; set; } = null!;
        public string? SlotQrCode { get; set; }
        public decimal Capacity { get; set; }
        public decimal CurrentCapacity { get; set; }
        public decimal RemainingCapacity { get; set; }

        /// <summary>Slot chỉ được chứa 1 ProductVariant. Nếu slot trống thì null.</summary>
        public int? ProductVariantId { get; set; }
        public string? ProductName { get; set; }
        public string? VariantName { get; set; }

        public int BoxCount { get; set; }
        public decimal TotalBoxWeight { get; set; }
        public List<SlotBoxItemDto> Boxes { get; set; } = new();
    }
}

