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
        public int PurchaseOrderDetailId { get; set; }
        public PurchaseOrderDetail PurchaseOrderDetail { get; set; } = null!;
        /// <summary>Khối lượng đặt theo PO (từ PurchaseOrderDetail).</summary>
        public decimal OrderedWeight { get; set; }
        /// <summary>Khối lượng thực nhận do kho nhập (warehouse chỉ nhập ReceivedWeight).</summary>
        public decimal ReceivedWeight { get; set; }
        public decimal RejectWeight { get; private set; }
        public void CalculateRejectWeight()
        {
            RejectWeight = ReceivedWeight - (UsableWeight ?? 0);
        }
        public decimal? UsableWeight { get; set; } // Số lượng sử dụng được sau khi QC (kg)

        public QCResult QCResult { get; set; } = QCResult.Pending;
        public string? QCNote { get; set; }
        public string? InspectedBy { get; set; } // Tên người QC
        public ApplicationUser? InspectedUser { get; set; }
        public DateTime? InspectedAt { get; set; }
        public decimal UnitPrice { get; set; }

        // ===== LOT (1 Detail -> N Lot) =====
        public ICollection<Lot> Lots { get; set; }
            = new List<Lot>();
    }
}
