using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgriIDMS.Domain.Entities
{
    public class ExportDetail
    {
        public int Id { get; set; }

        public int ExportReceiptId { get; set; }
        public ExportReceipt ExportReceipt { get; set; } = null!;

        public int BoxId { get; set; }
        public Box Box { get; set; } = null!;

        public decimal ActualQuantity { get; set; }
    }
}
