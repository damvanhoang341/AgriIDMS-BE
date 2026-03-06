using AgriIDMS.Application.DTOs.Supplier;
using AgriIDMS.Application.Interfaces;
using AgriIDMS.Application.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AgriIDMS.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SuppliersController : ControllerBase
    {
        private readonly ISupplierService _supplierService;

        public SuppliersController(ISupplierService supplierService)
        {
            _supplierService = supplierService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var suppliers = await _supplierService.GetAllSuppliersAsync();
            return Ok(suppliers);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var supplier = await _supplierService.GetSupplierByIdAsync(id);
            return Ok(supplier);
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateSupplierRequest request)
        {
            await _supplierService.CreateSupplierAsync(request);
            return NoContent();
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, UpdateSupplierRequest request)
        {
            await _supplierService.UpdateSupplierAsync(id, request);
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _supplierService.DeleteSupplierAsync(id);
            return NoContent();
        }
    }
}
