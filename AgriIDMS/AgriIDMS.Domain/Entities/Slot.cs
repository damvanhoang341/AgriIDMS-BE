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

        public decimal Capacity { get; set; }
        public decimal CurrentCapacity { get; set; }

        public int RackId { get; set; }
        public Rack Rack { get; set; } = null!;

        public ICollection<Box> Boxes { get; set; } = new List<Box>();
    }
}
