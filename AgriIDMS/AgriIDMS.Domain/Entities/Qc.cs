using AgriIDMS.Domain.Enums;

namespace AgriIDMS.Domain.Entities
{
    /// <summary>Kết quả kiểm tra chất lượng cho một dòng chi tiết phiếu nhập (1–1 với GoodsReceiptDetail).</summary>
    public class QcRecord
    {
        public int Id { get; set; }

        public int GoodsReceiptDetailId { get; set; }
        public GoodsReceiptDetail GoodsReceiptDetail { get; set; } = null!;

        /// <summary>Tổng khối lượng đã kiểm.</summary>
        public decimal InspectedWeight { get; set; }

        /// <summary>Khối lượng hỏng.</summary>
        public decimal DamagedWeight { get; set; }

        /// <summary>Khối lượng đạt = InspectedWeight - DamagedWeight.</summary>
        public decimal PassedWeight { get; set; }

        public QCResult QCResult { get; set; } = QCResult.Pending;
        public string? QCNote { get; set; }
        public string? InspectedBy { get; set; }
        public DateTime? InspectedAt { get; set; }

        public ICollection<QcClassificationDetail> ClassificationDetails { get; set; } =
            new List<QcClassificationDetail>();
    }
}
