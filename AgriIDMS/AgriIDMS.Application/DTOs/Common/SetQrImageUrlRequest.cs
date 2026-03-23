using System.ComponentModel.DataAnnotations;

namespace AgriIDMS.Application.DTOs.Common
{
    /// <summary>FE upload ảnh QR lên Cloudinary rồi gửi URL về DB.</summary>
    public class SetQrImageUrlRequest
    {
        [Required]
        [MaxLength(500)]
        public string QrImageUrl { get; set; } = "";
    }
}
