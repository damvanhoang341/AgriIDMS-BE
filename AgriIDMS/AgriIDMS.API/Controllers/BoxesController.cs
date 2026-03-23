using AgriIDMS.Application.DTOs.Box;
using AgriIDMS.Application.DTOs.Common;
using AgriIDMS.Application.Interfaces;
using AgriIDMS.Application.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AgriIDMS.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    //[Authorize]
    public class BoxesController : ControllerBase
    {
        private readonly IBoxService _boxService;

        public BoxesController(IBoxService boxService)
        {
            _boxService = boxService;
        }

        /// <summary>Gán box vào vị trí slot. Box và slot phải cùng kho; slot phải đủ dung lượng. Nếu kho lạnh sẽ ghi nhận PlacedInColdAt.</summary>
        [HttpPost("assign-slot")]
        //[Authorize(Roles = "Admin,Manager,WarehouseStaff")]
        public async Task<IActionResult> AssignBoxToSlot([FromBody] AssignBoxToSlotRequest request)
        {
            await _boxService.AssignBoxToSlotAsync(request);
            return Ok(new { Message = "Đã gán box vào slot thành công" });
        }

        /// <summary>Gán nhiều box vào cùng một slot trong một lần (nhanh hơn gọi assign-slot từng box).</summary>
        [HttpPost("assign-slot-batch")]
        //[Authorize(Roles = "Admin,Manager,WarehouseStaff")]
        public async Task<IActionResult> AssignBoxesToSlot([FromBody] AssignBoxesToSlotRequest request)
        {
            await _boxService.AssignBoxesToSlotAsync(request);
            return Ok(new { Message = "Đã gán box vào slot thành công", AssignedCount = request.BoxIds.Count });
        }

        /// <summary>Chuyển box đã xếp từ slot hiện tại sang slot khác (cùng kho) và ghi InventoryTransactionType.Transfer.</summary>
        [HttpPost("transfer-slot")]
        //[Authorize(Roles = "Admin,Manager,WarehouseStaff")]
        public async Task<IActionResult> TransferBoxToSlot([FromBody] TransferBoxToSlotRequest request)
        {
            var userId =
                User.FindFirstValue(ClaimTypes.NameIdentifier) ??
                User.FindFirstValue("sub") ??
                User.FindFirstValue("userId") ??
                User.FindFirstValue("id");

            // Nếu không có userId, tạo InventoryTransaction sẽ lỗi FK. Trả lỗi rõ ràng để FE biết cần đăng nhập.
            if (string.IsNullOrWhiteSpace(userId))
                throw new UnauthorizedException("Bạn cần đăng nhập để chuyển slot.");

            await _boxService.TransferBoxToSlotAsync(request, userId);
            return Ok(new { Message = "Đã chuyển box sang slot mới" });
        }

        /// <summary>Cập nhật hoặc xoá QR của box (nếu qrCode = null/empty).</summary>
        [HttpPut("{boxId:int}/qr")]
        //[Authorize(Roles = "Admin,Manager,WarehouseStaff")]
        public async Task<IActionResult> UpdateQrCode(int boxId, [FromBody] string? qrCode)
        {
            await _boxService.UpdateQrCodeAsync(boxId, qrCode);
            return Ok(new { Message = "Cập nhật QR cho box thành công" });
        }

        /// <summary>FE upload ảnh QR lên Cloudinary rồi gửi URL lưu DB.</summary>
        [HttpPut("{boxId:int}/qr-image")]
        public async Task<IActionResult> SetQrImage(int boxId, [FromBody] SetQrImageUrlRequest request)
        {
            await _boxService.UpdateQrImageUrlAsync(boxId, request.QrImageUrl);
            return Ok(new { message = "Đã cập nhật ảnh QR cho box." });
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

        /// <summary>Lấy danh sách box thuộc warehouse nhưng chưa được gán vào slot (SlotId = null).</summary>
        [HttpGet("unassigned")]
        public async Task<IActionResult> GetUnassignedBoxesByWarehouse([FromQuery] int warehouseId)
        {
            var boxes = await _boxService.GetUnassignedBoxesByWarehouseAsync(warehouseId);
            return Ok(boxes);
        }
    }
}
