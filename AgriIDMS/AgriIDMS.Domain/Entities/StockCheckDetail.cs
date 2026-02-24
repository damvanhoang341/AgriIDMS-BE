using AgriIDMS.Domain.Enums;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgriIDMS.Domain.Entities
{
    public class StockCheckDetail
    {
        public int Id { get; set; }

        public int StockCheckId { get; set; }
        public StockCheck StockCheck { get; set; }

        public int BoxId { get; set; }
        public Box Box { get; set; }

        // Snapshot data (RẤT QUAN TRỌNG)
        public decimal SnapshotWeight { get; set; }

        // Current system weight (tại thời điểm approve)
        public decimal? CurrentSystemWeight { get; set; }

        // Counted data
        public decimal? CountedWeight { get; set; }

        public decimal? DifferenceWeight { get; set; }

        public VarianceType? VarianceType { get; set; }

        public string? CountedBy { get; set; }
        public ApplicationUser? CountedUser { get; set; }

        public DateTime? CountedAt { get; set; }

        public string? Note { get; set; }
    }
}
