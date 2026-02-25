using AgriIDMS.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgriIDMS.Domain.Entities
{
    public class Refund
    {
        public int Id { get; set; }

        public int PaymentId { get; set; }
        public Payment Payment { get; set; } = null!;

        public decimal Amount { get; set; }

        public RefundStatus Status { get; set; }

        public string? RefundTransactionCode { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime? CompletedAt { get; set; }
        public int? ComplaintId { get; set; }
        public Complaint? Complaint { get; set; }
    }
}
