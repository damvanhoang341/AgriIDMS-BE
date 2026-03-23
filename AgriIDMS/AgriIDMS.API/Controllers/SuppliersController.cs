using AgriIDMS.Application.DTOs.Supplier;
using AgriIDMS.Application.Interfaces;
using AgriIDMS.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AgriIDMS.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
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
        [Authorize(Roles = "Admin,Manager,PurchasingStaff")]
        public async Task<IActionResult> Create(CreateSupplierRequest request)
        {
            await _supplierService.CreateSupplierAsync(request);
            return NoContent();
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,Manager,PurchasingStaff")]
        public async Task<IActionResult> Update(int id, UpdateSupplierRequest request)
        {
            await _supplierService.UpdateSupplierAsync(id, request);
            return NoContent();
        }

        [HttpPatch("{id}/status")]
        [Authorize(Roles = "Admin,Manager,PurchasingStaff")]
        public async Task<IActionResult> UpdateStatus(int id, UpdateStatusSupplierRequest request)
        {
            await _supplierService.UpdateStatusSupplierAsync(id, request);
            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Delete(int id)
        {
            await _supplierService.DeleteSupplierAsync(id);
            return NoContent();
        }
    }
}
