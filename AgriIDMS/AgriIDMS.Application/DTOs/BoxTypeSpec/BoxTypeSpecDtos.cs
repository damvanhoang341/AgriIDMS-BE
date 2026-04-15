using AgriIDMS.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace AgriIDMS.Application.DTOs.BoxTypeSpec
{
    public class BoxTypeSpecDto
    {
        public int Id { get; set; }
        public BoxType BoxType { get; set; }
        public string DisplayName { get; set; } = null!;
        public decimal LengthCm { get; set; }
        public decimal WidthCm { get; set; }
        public decimal HeightCm { get; set; }
        public decimal VolumeM3 { get; set; }
    }

    public class UpsertBoxTypeSpecItemRequest
    {
        public int? Id { get; set; }
        [Required]
        public BoxType BoxType { get; set; }
        [Required]
        [MaxLength(150)]
        public string DisplayName { get; set; } = null!;
        [Range(0, double.MaxValue)]
        public decimal LengthCm { get; set; }
        [Range(0, double.MaxValue)]
        public decimal WidthCm { get; set; }
        [Range(0, double.MaxValue)]
        public decimal HeightCm { get; set; }
    }
}

