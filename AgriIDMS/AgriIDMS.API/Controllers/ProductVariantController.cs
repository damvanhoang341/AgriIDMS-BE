using AgriIDMS.Application.DTOs.ProductVariant;
using AgriIDMS.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AgriIDMS.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ProductVariantController : ControllerBase
    {
        private readonly IProductVariantService _service;

        public ProductVariantController(IProductVariantService service)
        {
            _service = service;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAll()
        {
            var data = await _service.GetAllAsync();
            return Ok(data);
        }

        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetById(int id)
        {
            var data = await _service.GetByIdAsync(id);
            return Ok(data);
        }

        [HttpPost]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Create(CreateProductVariantDto dto)
        {
            var id = await _service.CreateAsync(dto);
            return Ok(id);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Update(int id, UpdateProductVariantDto dto)
        {
            await _service.UpdateAsync(id, dto);
            return Ok("Updated");
        }

        [HttpPatch("{id}")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> UpdateStatus(int id, UpdateProductVariantStatusDto dto)
        {
            await _service.UpdateStatusAsync(id, dto);
            return Ok("Updated");
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Delete(int id)
        {
            await _service.DeleteAsync(id);
            return Ok("Deleted");
        }

        /// <summary>
        /// Manager đặt % giảm giá thủ công khi tồn gần hết hạn (ghi đè Pricing:NearExpiryDiscountPercent).
        /// Gửi <c>discountPercent: null</c> để xóa override.
        /// </summary>
        [HttpPut("{id:int}/near-expiry-discount")]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> SetManualNearExpiryDiscount(int id, [FromBody] SetManualNearExpiryDiscountRequestDto? request)
        {
            await _service.SetManualNearExpiryDiscountAsync(id, request?.DiscountPercent);
            return Ok(new
            {
                message = "Đã cập nhật giảm giá gần hết hạn cho biến thể.",
                productVariantId = id,
                discountPercent = request?.DiscountPercent
            });
        }
    }
}
