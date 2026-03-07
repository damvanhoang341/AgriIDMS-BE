using AgriIDMS.Application.DTOs.Warehouse;
using AgriIDMS.Application.Interfaces;
using AgriIDMS.Domain.Exceptions;
using AgriIDMS.Application.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgriIDMS.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WarehousesController : ControllerBase
    {
        private readonly IWarehouseService _warehouseService;

        public WarehousesController(IWarehouseService warehouseService)
        {
            _warehouseService = warehouseService;
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Create([FromBody] CreateWarehouseRequest request)
        {
            try
            {
                var id = await _warehouseService.CreateAsync(request);

                return Ok(new
                {
                    message = "Tạo kho thành công",
                    id
                });
            }
            catch (InvalidBusinessRuleException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
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
            try
            {
                var warehouse = await _warehouseService.GetByIdAsync(id);
                return Ok(warehouse);
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update([FromRoute] int id, [FromBody] CreateWarehouseRequest request)
        {
            try
            {
                await _warehouseService.UpdateAsync(id, request);
                return Ok(new { message = "Cập nhật kho thành công" });
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
        public async Task<IActionResult> Delete([FromRoute] int id)
        {
            try
            {
                await _warehouseService.DeleteAsync(id);
                return Ok(new { message = "Xóa kho thành công" });
            }
            catch (NotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }
    }
}

