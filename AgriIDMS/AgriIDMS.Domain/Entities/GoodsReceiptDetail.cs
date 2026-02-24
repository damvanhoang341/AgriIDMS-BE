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

        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;

        public decimal EstimatedQuantity { get; set; }
        public decimal? ActualQuantity { get; set; }

        public decimal UnitPrice { get; set; }
        public DateTime ExpiryDate { get; set; }

        public QCResult QCResult { get; set; } = QCResult.Pending;

        public string? QCNote { get; set; }

        public string? InspectedBy { get; set; }
        public ApplicationUser? InspectedUser { get; set; }

        public DateTime? InspectedAt { get; set; }

        public Lot? Lot { get; set; }
    }
}
