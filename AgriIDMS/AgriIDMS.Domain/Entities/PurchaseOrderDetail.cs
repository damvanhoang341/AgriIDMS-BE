namespace AgriIDMS.Domain.Entities
{
    public class PurchaseOrderDetail
    {
        public int Id { get; set; }

        public int PurchaseOrderId { get; set; }
        public PurchaseOrder PurchaseOrder { get; set; } = null!;

        public int ProductVariantId { get; set; }
        public ProductVariant ProductVariant { get; set; } = null!;
        public decimal OrderedWeight { get; set; }
        /// <summary>Dung sai hao hụt (%) cho dòng đặt hàng.</summary>
        public decimal TolerancePercent { get; set; }
        /// <summary>Tổng khối lượng đã nhận cho dòng PO (chỉ cập nhật khi phiếu nhập Approved).</summary>
        public decimal ReceivedWeight { get; set; }
        public decimal UnitPrice { get; set; }

        /// <summary>Ngày thu hoạch nông sản cho dòng đơn mua này.</summary>
        public DateTime HarvestDate { get; set; }

        public decimal RemainingWeight => OrderedWeight - ReceivedWeight;

        public ICollection<GoodsReceiptDetail> GoodsReceiptDetails { get; set; } = new List<GoodsReceiptDetail>();
    }
}
