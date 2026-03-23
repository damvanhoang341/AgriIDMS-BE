namespace AgriIDMS.Application.DTOs.GoodsReceipt
{
    /// <summary>Kết quả sau khi tạo box — FE dùng để tạo ảnh QR và gọi PUT qr-image.</summary>
    public class BoxCreatedItemDto
    {
        public int Id { get; set; }
        public string BoxCode { get; set; } = "";
        /// <summary>Nội dung quét (trùng cột QRCode).</summary>
        public string QrPayload { get; set; } = "";
    }
}
