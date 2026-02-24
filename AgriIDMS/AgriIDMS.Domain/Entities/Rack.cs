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

        public int ZoneId { get; set; }
        public Zone Zone { get; set; } = null!;

        public ICollection<Slot> Slots { get; set; } = new List<Slot>();
    }
}
