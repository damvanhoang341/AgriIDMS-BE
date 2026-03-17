using AgriIDMS.Application.DTOs.Warehouse;
using AgriIDMS.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgriIDMS.API.Controllers
{
    [Route("api/warehouses/{warehouseId:int}/[controller]")]
    [ApiController]
    //[Authorize]
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
        //[Authorize(Roles = "Admin,Manager,WarehouseStaff")]
        public async Task<IActionResult> Create([FromRoute] int warehouseId, [FromBody] CreateZoneRequest request)
        {
            var id = await _zoneService.CreateAsync(warehouseId, request);
            return Ok(new { message = "Tạo zone thành công", id });
        }

        [HttpPut("{id:int}")]
        //[Authorize(Roles = "Admin,Manager,WarehouseStaff")]
        public async Task<IActionResult> Update([FromRoute] int warehouseId, [FromRoute] int id, [FromBody] CreateZoneRequest request)
        {
            await _zoneService.UpdateAsync(id, request);
            return Ok(new { message = "Cập nhật zone thành công" });
        }

        [HttpDelete("{id:int}")]
        //[Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Delete([FromRoute] int warehouseId, [FromRoute] int id)
        {
            await _zoneService.DeleteAsync(id);
            return Ok(new { message = "Xóa zone thành công" });
        }
    }
}
