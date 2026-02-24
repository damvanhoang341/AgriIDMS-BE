using AgriIDMS.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgriIDMS.Domain.Entities
{
    public class Supplier
    {
        public int Id { get; set; }

        public string Name { get; set; } = null!;

        public string? ContactPerson { get; set; }

        public string? Phone { get; set; }

        public string? Email { get; set; }

        public string? Address { get; set; }

        public SupplierStatus Status { get; set; } = SupplierStatus.Active;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<GoodsReceipt> GoodsReceipts { get; set; }
            = new List<GoodsReceipt>();
    }
}
