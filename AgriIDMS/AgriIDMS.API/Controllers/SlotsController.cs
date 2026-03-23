using AgriIDMS.Application.DTOs.Common;
using AgriIDMS.Application.DTOs.Warehouse;
using AgriIDMS.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgriIDMS.API.Controllers
{
    [Route("api/racks/{rackId:int}/[controller]")]
    [ApiController]
    //[Authorize]
    public class SlotsController : ControllerBase
    {
        private readonly ISlotService _slotService;

        public SlotsController(ISlotService slotService)
        {
            _slotService = slotService;
        }

        [HttpGet]
        public async Task<IActionResult> GetByRack([FromRoute] int rackId)
        {
            var slots = await _slotService.GetByRackAsync(rackId);
            return Ok(slots);
        }

        [HttpPost]
        //[Authorize(Roles = "Admin,Manager,WarehouseStaff")]
        public async Task<IActionResult> Create([FromRoute] int rackId, [FromBody] CreateSlotRequest request)
        {
            var id = await _slotService.CreateAsync(rackId, request);
            return Ok(new { message = "Tạo slot thành công", id });
        }

        [HttpGet("~/api/slots/by-qr/{qrCode}")]
        public async Task<IActionResult> GetByQrCode(string qrCode)
        {
            var slot = await _slotService.GetByQrCodeAsync(qrCode);
            if (slot == null)
                return NotFound(new { message = "Slot không tồn tại hoặc QR không hợp lệ" });
            return Ok(slot);
        }

        /// <summary>FE upload ảnh QR lên Cloudinary rồi gửi URL lưu DB.</summary>
        [HttpPut("~/api/slots/{id:int}/qr-image")]
        public async Task<IActionResult> SetQrImage(int id, [FromBody] SetQrImageUrlRequest request)
        {
            await _slotService.UpdateQrImageUrlAsync(id, request.QrImageUrl);
            return Ok(new { message = "Đã cập nhật ảnh QR cho slot." });
        }

        /// <summary>Lấy chi tiết slot đang chứa gì (product/variant + danh sách box). Slot chỉ được chứa 1 loại sản phẩm.</summary>
        [HttpGet("~/api/slots/{slotId:int:min(1)}/contents")]
        public async Task<IActionResult> GetContents(int slotId)
        {
            var result = await _slotService.GetContentsAsync(slotId);
            return Ok(result);
        }

        [HttpPut("{id:int}")]
        //[Authorize(Roles = "Admin,Manager,WarehouseStaff")]
        public async Task<IActionResult> Update([FromRoute] int rackId, [FromRoute] int id, [FromBody] CreateSlotRequest request)
        {
            await _slotService.UpdateAsync(id, request);
            return Ok(new { message = "Cập nhật slot thành công" });
        }

        [HttpDelete("{id:int}")]
        //[Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Delete([FromRoute] int rackId, [FromRoute] int id)
        {
            await _slotService.DeleteAsync(id);
            return Ok(new { message = "Xóa slot thành công" });
        }
    }
}
