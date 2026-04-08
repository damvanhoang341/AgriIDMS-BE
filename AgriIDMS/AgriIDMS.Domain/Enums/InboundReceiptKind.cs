namespace AgriIDMS.Domain.Enums
{
    /// <summary>Phân loại phiếu nhập kho (in & chứng từ).</summary>
    public enum InboundReceiptKind
    {
        /// <summary>Theo đơn mua (PO).</summary>
        FromPurchaseOrder = 0,

        /// <summary>Nhập không gắn PO (hoặc ngoài PO).</summary>
        DirectInbound = 1
    }
}
