using System.Threading.Tasks;

namespace AgriIDMS.Application.Interfaces
{
    /// <summary>Abstraction cho service sinh QR code để Application layer không phụ thuộc trực tiếp vào thư viện bên ngoài.</summary>
    public interface IQrCodeGenerator
    {
        /// <summary>
        /// Sinh QR code cho nội dung text.
        /// Trả về chuỗi (ví dụ: Base64 PNG) để FE hiển thị hoặc in tem.
        /// </summary>
        Task<string> GenerateAsync(string content);
    }
}

