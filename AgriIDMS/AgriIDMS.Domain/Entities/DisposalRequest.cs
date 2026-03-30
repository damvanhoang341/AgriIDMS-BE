using AgriIDMS.Domain.Enums;
using System;
using System.Collections.Generic;

namespace AgriIDMS.Domain.Entities
{
    public class DisposalRequest
    {
        public int Id { get; set; }

        public int WarehouseId { get; set; }
        public Warehouse Warehouse { get; set; } = null!;

        public DisposalRequestStatus Status { get; set; } = DisposalRequestStatus.Pending;

        public string Reason { get; set; } = string.Empty;

        public string RequestedBy { get; set; } = string.Empty;
        public ApplicationUser RequestedUser { get; set; } = null!;
        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

        public string? ReviewedBy { get; set; }
        public ApplicationUser? ReviewedUser { get; set; }
        public DateTime? ReviewedAt { get; set; }

        public string? ReviewNote { get; set; }

        public ICollection<DisposalRequestItem> Items { get; set; } = new List<DisposalRequestItem>();
    }

    public class DisposalRequestItem
    {
        public int Id { get; set; }

        public int DisposalRequestId { get; set; }
        public DisposalRequest DisposalRequest { get; set; } = null!;

        public int BoxId { get; set; }
        public Box Box { get; set; } = null!;
    }
}

