using System.ComponentModel.DataAnnotations;

namespace AgriIDMS.Application.DTOs.Warehouse
{
    public class CreateZoneRequest
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = null!;
    }

    public class ZoneDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public int WarehouseId { get; set; }
    }

    public class CreateRackRequest
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = null!;
    }

    public class RackDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public int ZoneId { get; set; }
    }

    public class CreateSlotRequest
    {
        [Required]
        [MaxLength(100)]
        public string Code { get; set; } = null!;

        [Range(0.0001, double.MaxValue)]
        public decimal Capacity { get; set; }

        [MaxLength(200)]
        public string? QrCode { get; set; }
    }

    public class SlotDto
    {
        public int Id { get; set; }
        public string Code { get; set; } = null!;
        public string? QrCode { get; set; }
        public string? QrImageUrl { get; set; }
        public int? ProductVariantId { get; set; }
        public string? ProductVariantName { get; set; }
        public string? ProductName { get; set; }
        public decimal Capacity { get; set; }
        public decimal CurrentCapacity { get; set; }
        /// <summary>Name của rack chứa slot (phục vụ màn scan nhanh).</summary>
        public string? RackName { get; set; }
        public int RackId { get; set; }
    }
}

