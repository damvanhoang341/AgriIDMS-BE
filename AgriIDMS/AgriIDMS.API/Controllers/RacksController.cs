using AgriIDMS.Application.DTOs.Warehouse;
using AgriIDMS.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgriIDMS.API.Controllers
{
    [Route("api/zones/{zoneId:int}/[controller]")]
    [ApiController]
    [Authorize]
    public class RacksController : ControllerBase
    {
        private readonly IRackService _rackService;

        public RacksController(IRackService rackService)
        {
            _rackService = rackService;
        }

        [HttpGet]
        public async Task<IActionResult> GetByZone([FromRoute] int zoneId)
        {
            var racks = await _rackService.GetByZoneAsync(zoneId);
            return Ok(racks);
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Manager,WarehouseStaff")]
        public async Task<IActionResult> Create([FromRoute] int zoneId, [FromBody] CreateRackRequest request)
        {
            var id = await _rackService.CreateAsync(zoneId, request);
            return Ok(new { message = "Tạo rack thành công", id });
        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin,Manager,WarehouseStaff")]
        public async Task<IActionResult> Update([FromRoute] int zoneId, [FromRoute] int id, [FromBody] CreateRackRequest request)
        {
            await _rackService.UpdateAsync(id, request);
            return Ok(new { message = "Cập nhật rack thành công" });
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Delete([FromRoute] int zoneId, [FromRoute] int id)
        {
            await _rackService.DeleteAsync(id);
            return Ok(new { message = "Xóa rack thành công" });
        }
    }
}
