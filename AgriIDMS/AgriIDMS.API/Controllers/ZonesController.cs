using AgriIDMS.Application.DTOs.Warehouse;
using AgriIDMS.Application.Exceptions;
using AgriIDMS.Application.Interfaces;
using AgriIDMS.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace AgriIDMS.API.Controllers
{
    [Route("api/warehouses/{warehouseId:int}/[controller]")]
    [ApiController]
    public class ZonesController : ControllerBase
    {
        private readonly IZoneService _zoneService;

        public ZonesController(IZoneService zoneService)
        {
            _zoneService = zoneService;
        }

        [HttpGet]
        public async Task<IActionResult> GetByWarehouse([FromRoute] int warehouseId)
        {
            var zones = await _zoneService.GetByWarehouseAsync(warehouseId);
            return Ok(zones);
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromRoute] int warehouseId, [FromBody] CreateZoneRequest request)
        {
            try
            {
                var id = await _zoneService.CreateAsync(warehouseId, request);
                return Ok(new { message = "Tạo zone thành công", id });
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
        public async Task<IActionResult> Update([FromRoute] int warehouseId, [FromRoute] int id, [FromBody] CreateZoneRequest request)
        {
            try
            {
                await _zoneService.UpdateAsync(id, request);
                return Ok(new { message = "Cập nhật zone thành công" });
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
        public async Task<IActionResult> Delete([FromRoute] int warehouseId, [FromRoute] int id)
        {
            try
            {
                await _zoneService.DeleteAsync(id);
                return Ok(new { message = "Xóa zone thành công" });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }
    }
}

