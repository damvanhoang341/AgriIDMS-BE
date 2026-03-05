using AgriIDMS.Application.DTOs.Warehouse;
using AgriIDMS.Application.Exceptions;
using AgriIDMS.Application.Interfaces;
using AgriIDMS.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace AgriIDMS.API.Controllers
{
    [Route("api/zones/{zoneId:int}/[controller]")]
    [ApiController]
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
        public async Task<IActionResult> Create([FromRoute] int zoneId, [FromBody] CreateRackRequest request)
        {
            try
            {
                var id = await _rackService.CreateAsync(zoneId, request);
                return Ok(new { message = "Tạo rack thành công", id });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidBusinessRuleException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update([FromRoute] int zoneId, [FromRoute] int id, [FromBody] CreateRackRequest request)
        {
            try
            {
                await _rackService.UpdateAsync(id, request);
                return Ok(new { message = "Cập nhật rack thành công" });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidBusinessRuleException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete([FromRoute] int zoneId, [FromRoute] int id)
        {
            try
            {
                await _rackService.DeleteAsync(id);
                return Ok(new { message = "Xóa rack thành công" });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }
    }
}

