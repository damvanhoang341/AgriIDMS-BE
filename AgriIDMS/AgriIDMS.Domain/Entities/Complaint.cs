using AgriIDMS.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgriIDMS.Domain.Entities
{
    public class Complaint : BaseEntity
    {
        public int OrderId { get; set; }
        public int BoxId { get; set; }

        public ComplaintType Type { get; set; }
        public ComplaintStatus Status { get; set; }

        public decimal DamagedQuantity { get; set; }
        public string? Description { get; set; }

        public string? CustomerEvidenceUrl { get; set; }

        public bool IsVerified { get; set; }
        public string? VerifiedBy { get; set; }
        public DateTime? VerifiedAt { get; set; }

        public DateTime CreatedAt { get; set; }

        // Navigation
        public Order Order { get; set; } = null!;
        public Box Box { get; set; } = null!;
        public ApplicationUser? VerifiedUser { get; set; }

        public ICollection<Refund> Refunds { get; set; } = new List<Refund>();
    }
}
