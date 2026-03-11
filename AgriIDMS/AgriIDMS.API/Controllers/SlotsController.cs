using AgriIDMS.Application.DTOs.Warehouse;
using AgriIDMS.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgriIDMS.API.Controllers
{
    [Route("api/racks/{rackId:int}/[controller]")]
    [ApiController]
    [Authorize]
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
        [Authorize(Roles = "Admin,Manager,WarehouseStaff")]
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

        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin,Manager,WarehouseStaff")]
        public async Task<IActionResult> Update([FromRoute] int rackId, [FromRoute] int id, [FromBody] CreateSlotRequest request)
        {
            await _slotService.UpdateAsync(id, request);
            return Ok(new { message = "Cập nhật slot thành công" });
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Delete([FromRoute] int rackId, [FromRoute] int id)
        {
            await _slotService.DeleteAsync(id);
            return Ok(new { message = "Xóa slot thành công" });
        }
    }
}
