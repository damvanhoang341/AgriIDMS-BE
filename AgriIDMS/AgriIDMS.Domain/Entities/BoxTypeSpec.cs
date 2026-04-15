using AgriIDMS.Domain.Enums;

namespace AgriIDMS.Domain.Entities
{
    public class BoxTypeSpec
    {
        public int Id { get; set; }
        public BoxType BoxType { get; set; }
        public string DisplayName { get; set; } = null!;
        public decimal LengthCm { get; set; }
        public decimal WidthCm { get; set; }
        public decimal HeightCm { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}

