using System;

namespace AgriIDMS.Application.DTOs.Box
{
    public class UnassignedBoxDto
    {
        public int Id { get; set; }
        public string BoxCode { get; set; } = null!;
        public string? QrCode { get; set; }
        public string? QrImageUrl { get; set; }
        public decimal Weight { get; set; }
        public string Status { get; set; } = null!;

        public int? SlotId { get; set; }
        public int? WarehouseId { get; set; }
        public int LotId { get; set; }
        public int? ProductVariantId { get; set; }
        public string? ProductVariantName { get; set; }
        public string? ProductName { get; set; }

        public DateTime? PlacedInColdAt { get; set; }
    }
}

