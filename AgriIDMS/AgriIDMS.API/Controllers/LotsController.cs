using AgriIDMS.Application.DTOs.Common;
using AgriIDMS.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AgriIDMS.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LotsController : ControllerBase
    {
        private readonly ILotService _lotService;

        public LotsController(ILotService lotService)
        {
            _lotService = lotService;
        }

        [HttpGet("by-goods-receipt/{goodsReceiptId}")]
        public async Task<IActionResult> GetLotsByGoodsReceiptId(int goodsReceiptId)
        {
            var lots = await _lotService.GetLotsByGoodsReceiptIdAsync(goodsReceiptId);

            return Ok(lots);
        }

        /// <summary>Tra cứu Lot theo payload QR (QR Lot = LotCode).</summary>
        [HttpGet("by-qr/{qrCode}")]
        public async Task<IActionResult> GetByQrCode(string qrCode)
        {
            var lot = await _lotService.GetByLotCodeAsync(qrCode);
            if (lot == null)
                return NotFound(new { message = "Không tìm thấy lot theo QR code" });

            return Ok(lot);
        }

        /// <summary>FE upload ảnh QR lên Cloudinary rồi gửi URL lưu DB.</summary>
        [HttpPut("{id:int}/qr-image")]
        public async Task<IActionResult> SetQrImage(int id, [FromBody] SetQrImageUrlRequest request)
        {
            await _lotService.UpdateQrImageUrlAsync(id, request.QrImageUrl);
            return Ok(new { message = "Đã cập nhật ảnh QR cho lot." });
        }
    }
}
