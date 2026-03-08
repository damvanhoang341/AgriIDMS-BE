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
        // / <summary>Khối lượng tối đa có thể chứa trong slot (kg).</summary>
        public decimal Capacity { get; set; }
        /// <summary>Khối lượng hiện tại đã chứa trong slot (kg).</summary>
        public decimal CurrentCapacity { get; set; }

        public int RackId { get; set; }
        public Rack Rack { get; set; } = null!;

        public ICollection<Box> Boxes { get; set; } = new List<Box>();
    }
}
