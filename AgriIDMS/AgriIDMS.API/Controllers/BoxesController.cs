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
    }
}
