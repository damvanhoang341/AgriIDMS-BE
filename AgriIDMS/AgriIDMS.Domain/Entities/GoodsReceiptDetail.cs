using AgriIDMS.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgriIDMS.Domain.Entities
{
    public class GoodsReceiptDetail
    {
        public int Id { get; set; }

        public int GoodsReceiptId { get; set; }
        public GoodsReceipt GoodsReceipt { get; set; } = null!;

        public int ProductVariantId { get; set; }
        public ProductVariant ProductVariant { get; set; } = null!;

        // ===== SỐ LƯỢNG =====
        public decimal EstimatedQuantity { get; set; }
        public decimal? ActualQuantity { get; set; }

        public decimal UnitPrice { get; set; }

        // ===== QC =====
        public QCResult QCResult { get; set; } = QCResult.Pending;
        public string? QCNote { get; set; }

        public string? InspectedBy { get; set; }
        public ApplicationUser? InspectedUser { get; set; }
        public DateTime? InspectedAt { get; set; }

        // ===== LOT (1 Detail -> N Lot) =====
        public ICollection<Lot> Lots { get; set; }
            = new List<Lot>();
    }
}
