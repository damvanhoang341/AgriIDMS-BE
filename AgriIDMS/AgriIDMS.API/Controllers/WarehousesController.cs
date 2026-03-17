using AgriIDMS.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgriIDMS.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    //[Authorize]
    public class WarehousesController : ControllerBase
    {
        private readonly IWarehouseService _warehouseService;

        public WarehousesController(IWarehouseService warehouseService)
        {
            _warehouseService = warehouseService;
        }

        [HttpPost]
        //[Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Create([FromBody] Application.DTOs.Warehouse.CreateWarehouseRequest request)
        {
            var id = await _warehouseService.CreateAsync(request);
            return Ok(new { message = "Tạo kho thành công", id });
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var warehouses = await _warehouseService.GetAllAsync();
            return Ok(warehouses);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById([FromRoute] int id)
        {
            var warehouse = await _warehouseService.GetByIdAsync(id);
            return Ok(warehouse);
        }

        [HttpPut("{id:int}")]
        //[Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Update([FromRoute] int id, [FromBody] Application.DTOs.Warehouse.CreateWarehouseRequest request)
        {
            await _warehouseService.UpdateAsync(id, request);
            return Ok(new { message = "Cập nhật kho thành công" });
        }

        [HttpDelete("{id:int}")]
        //[Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Delete([FromRoute] int id)
        {
            await _warehouseService.DeleteAsync(id);
            return Ok(new { message = "Xóa kho thành công" });
        }
    }
}
