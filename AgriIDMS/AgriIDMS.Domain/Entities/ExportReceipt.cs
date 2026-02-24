using AgriIDMS.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgriIDMS.Domain.Entities
{
    public class ExportReceipt
    {
        public int Id { get; set; }

        public string ExportCode { get; set; } = null!;

        public int OrderId { get; set; }
        public Order Order { get; set; } = null!;

        public ExportStatus Status { get; set; } = ExportStatus.PendingPick;

        public string CreatedBy { get; set; } = null!;
        public ApplicationUser CreatedUser { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<ExportDetail> Details { get; set; } = new List<ExportDetail>();
    }
}
