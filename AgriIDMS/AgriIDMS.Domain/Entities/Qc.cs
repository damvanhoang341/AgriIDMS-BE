using AgriIDMS.Domain.Enums;

namespace AgriIDMS.Domain.Entities
{
    /// <summary>Kết quả kiểm tra chất lượng cho một dòng chi tiết phiếu nhập (1–1 với GoodsReceiptDetail).</summary>
    public class Qc
    {
        public int Id { get; set; }

        public int GoodsReceiptDetailId { get; set; }
        public GoodsReceiptDetail GoodsReceiptDetail { get; set; } = null!;

        /// <summary>Khối lượng sử dụng được sau QC.</summary>
        public decimal UsableWeight { get; set; }

        public QCResult QCResult { get; set; } = QCResult.Pending;
        public string? QCNote { get; set; }
        public string? InspectedBy { get; set; }
        public DateTime? InspectedAt { get; set; }
    }
}
