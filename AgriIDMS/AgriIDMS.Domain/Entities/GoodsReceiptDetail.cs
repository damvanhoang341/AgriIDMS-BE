using AgriIDMS.Domain.Enums;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

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

        /// <summary>Kết quả QC (bảng riêng). Nếu null = chưa QC.</summary>
        public Qc? Qc { get; set; }

        /// <summary>Khối lượng sử dụng được sau QC. Trước QC = null.</summary>
        [NotMapped]
        public decimal? UsableWeight => Qc?.UsableWeight;

        /// <summary>Khối lượng loại (không âm). Trước QC trả về 0.</summary>
        [NotMapped]
        public decimal RejectWeight => Qc != null ? Math.Max(0, ReceivedWeight - Qc.UsableWeight) : 0;

        /// <summary>Khối lượng kỳ vọng từ PO (không lưu DB, lấy từ PurchaseOrderDetail.OrderedWeight).</summary>
        [NotMapped]
        public decimal ExpectedWeight => PurchaseOrderDetail?.OrderedWeight ?? 0;

        [NotMapped]
        public QCResult QCResult => Qc?.QCResult ?? QCResult.Pending;

        [NotMapped]
        public string? QCNote => Qc?.QCNote;

        [NotMapped]
        public string? InspectedBy => Qc?.InspectedBy;

        [NotMapped]
        public DateTime? InspectedAt => Qc?.InspectedAt;

        public decimal UnitPrice { get; set; }

        public ICollection<Lot> Lots { get; set; } = new List<Lot>();
    }
}
