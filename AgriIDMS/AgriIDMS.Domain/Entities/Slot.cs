using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgriIDMS.Domain.Entities
{
    public class Slot
    {
        public int Id { get; set; }
        public string Code { get; set; } = null!;
        public string? QrCode { get; set; }
        /// <summary>URL ảnh QR (Cloudinary) — do frontend tạo và gửi lên.</summary>
        public string? QrImageUrl { get; set; }
        /// <summary>Thể tích tối đa có thể chứa trong slot (m3).</summary>
        public decimal Capacity { get; set; }
        /// <summary>Thể tích hiện tại đã chứa trong slot (m3).</summary>
        public decimal CurrentCapacity { get; set; }
        public decimal? LengthCm { get; set; }
        public decimal? WidthCm { get; set; }
        public decimal? HeightCm { get; set; }
        public decimal? VolumeM3 { get; set; }

        public int RackId { get; set; }
        public Rack Rack { get; set; } = null!;

        public ICollection<Box> Boxes { get; set; } = new List<Box>();
    }
}
