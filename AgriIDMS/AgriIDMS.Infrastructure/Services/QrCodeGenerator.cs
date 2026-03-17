using System;
using System.IO;
using System.Threading.Tasks;
using AgriIDMS.Application.Interfaces;
using QRCoder;

namespace AgriIDMS.Infrastructure.Services
{
    /// <summary>Triển khai IQrCodeGenerator bằng thư viện QRCoder. Thuộc Infrastructure vì phụ thuộc thư viện ngoài.</summary>
    public class QrCodeGenerator : IQrCodeGenerator
    {
        public Task<string> GenerateAsync(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
                throw new ArgumentException("Nội dung QR không được để trống", nameof(content));

            using var qrGenerator = new QRCodeGenerator();
            using var qrData = qrGenerator.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q);
            using var qrCode = new PngByteQRCode(qrData);
            // Trả về Base64 của ảnh PNG để FE hiển thị bằng data URL
            byte[] pngBytes = qrCode.GetGraphic(20);
            string base64 = Convert.ToBase64String(pngBytes);
            return Task.FromResult(base64);
        }
    }
}

