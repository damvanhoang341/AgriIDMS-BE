using System;
using System.Threading.Tasks;
using AgriIDMS.Application.Interfaces;

namespace AgriIDMS.Infrastructure.Services
{
    /// <summary>
    /// Triển khai IQrCodeGenerator.
    /// Hiện tại để tránh lỗi tràn cột QRCode trong DB (nvarchar(300)), ta lưu payload text (BoxCode) làm nội dung QR,
    /// còn việc render ảnh QR sẽ do client hoặc endpoint riêng xử lý khi cần.
    /// </summary>
    public class QrCodeGenerator : IQrCodeGenerator
    {
        public Task<string> GenerateAsync(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                throw new ArgumentException("Nội dung QR không được để trống", nameof(content));

            // Lưu thẳng nội dung payload (ví dụ BoxCode) vào cột QRCode.
            // Độ dài ngắn (< 300) nên không bị SqlException truncation.
            return Task.FromResult(content);
        }
    }
}

