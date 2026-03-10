using AgriIDMS.Application.DTOs.Box;
using AgriIDMS.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgriIDMS.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class BoxesController : ControllerBase
    {
        private readonly IBoxService _boxService;

        public BoxesController(IBoxService boxService)
        {
            _boxService = boxService;
        }

        /// <summary>Gán box vào vị trí slot. Box và slot phải cùng kho; slot phải đủ dung lượng. Nếu kho lạnh sẽ ghi nhận PlacedInColdAt.</summary>
        [HttpPost("assign-slot")]
        [Authorize(Roles = "Admin,Manager,WarehouseStaff")]
        public async Task<IActionResult> AssignBoxToSlot([FromBody] AssignBoxToSlotRequest request)
        {
            await _boxService.AssignBoxToSlotAsync(request);
            return Ok(new { Message = "Đã gán box vào slot thành công" });
        }

        /// <summary>Cập nhật hoặc xoá QR của box (nếu qrCode = null/empty).</summary>
        [HttpPut("{boxId:int}/qr")]
        [Authorize(Roles = "Admin,Manager,WarehouseStaff")]
        public async Task<IActionResult> UpdateQrCode(int boxId, [FromBody] string? qrCode)
        {
            await _boxService.UpdateQrCodeAsync(boxId, qrCode);
            return Ok(new { Message = "Cập nhật QR cho box thành công" });
        }

        /// <summary>Lấy thông tin box theo QR (scan QR trên thùng).</summary>
        [HttpGet("by-qr/{qrCode}")]
        public async Task<IActionResult> GetByQrCode(string qrCode)
        {
            var box = await _boxService.GetByQrCodeAsync(qrCode);
            if (box == null)
                return NotFound(new { Message = "Box không tồn tại hoặc QR không hợp lệ" });

            return Ok(box);
        }
    }
}
