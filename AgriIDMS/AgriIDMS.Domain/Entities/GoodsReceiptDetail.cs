using AgriIDMS.Domain.Enums;
using System.Collections.Generic;

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

        public decimal ReceivedWeight { get; set; }
        /// <summary>Khối lượng sử dụng được sau QC. Trước QC = null.</summary>
        public decimal? UsableWeight { get; set; }
        /// <summary>Khối lượng loại (không âm). Trước QC trả về 0.</summary>
        public decimal RejectWeight => UsableWeight.HasValue ? Math.Max(0, ReceivedWeight - UsableWeight.Value) : 0;

        /// <summary>Khối lượng kỳ vọng từ PO (không lưu DB, lấy từ PurchaseOrderDetail.OrderedWeight).</summary>
        public decimal ExpectedWeight => PurchaseOrderDetail?.OrderedWeight ?? 0;

        public QCResult QCResult { get; set; } = QCResult.Pending;
        public string? QCNote { get; set; }
        public string? InspectedBy { get; set; }
        public DateTime? InspectedAt { get; set; }
        public decimal UnitPrice { get; set; }

        public ICollection<Lot> Lots { get; set; } = new List<Lot>();
    }
}
