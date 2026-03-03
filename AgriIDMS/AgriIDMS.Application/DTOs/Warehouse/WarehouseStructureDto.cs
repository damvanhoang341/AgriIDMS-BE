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
    }

    public class SlotDto
    {
        public int Id { get; set; }
        public string Code { get; set; } = null!;
        public string? QrCode { get; set; }
        public decimal Capacity { get; set; }
        public decimal CurrentCapacity { get; set; }
        public int RackId { get; set; }
    }
}

