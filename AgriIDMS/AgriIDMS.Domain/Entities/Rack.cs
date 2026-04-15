using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgriIDMS.Domain.Entities
{
    public class Rack
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public decimal? LengthM { get; set; }
        public decimal? WidthM { get; set; }
        public decimal? FloorAreaM2 { get; set; }

        public int ZoneId { get; set; }
        public Zone Zone { get; set; } = null!;

        public ICollection<Slot> Slots { get; set; } = new List<Slot>();
    }
}
