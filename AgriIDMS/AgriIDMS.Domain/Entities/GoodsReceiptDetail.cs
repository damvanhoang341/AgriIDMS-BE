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

        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;
        public int? ProductVariantId { get; set; }
        public ProductVariant? ProductVariant { get; set; }
        public int PurchaseOrderDetailId { get; set; }
        public PurchaseOrderDetail PurchaseOrderDetail { get; set; } = null!;
        public int? SupplierPlanDetailId { get; set; }
        public PurchaseOrderSupplierPlanDetail? SupplierPlanDetail { get; set; }

        public decimal ReceivedWeight { get; set; }

        /// <summary>Kết quả QC (bảng riêng). Nếu null = chưa QC.</summary>
        public QcRecord? QcRecord { get; set; }

        /// <summary>Khối lượng sử dụng được sau QC. Trước QC = null.</summary>
        [NotMapped]
        public decimal? UsableWeight => QcRecord?.PassedWeight;

        /// <summary>Khối lượng loại (không âm). Trước QC trả về 0.</summary>
        [NotMapped]
        public decimal RejectWeight => QcRecord?.DamagedWeight ?? 0;

        /// <summary>Khối lượng kỳ vọng từ PO (không lưu DB, lấy từ PurchaseOrderDetail.OrderedWeight).</summary>
        [NotMapped]
        public decimal ExpectedWeight => PurchaseOrderDetail?.OrderedWeight ?? 0;

        [NotMapped]
        public QCResult QCResult => QcRecord?.QCResult ?? QCResult.Pending;

        [NotMapped]
        public string? QCNote => QcRecord?.QCNote;

        [NotMapped]
        public string? InspectedBy => QcRecord?.InspectedBy;

        [NotMapped]
        public DateTime? InspectedAt => QcRecord?.InspectedAt;

        public decimal UnitPrice { get; set; }

        public ICollection<Lot> Lots { get; set; } = new List<Lot>();
    }
}
