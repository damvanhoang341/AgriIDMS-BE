using AgriIDMS.Application.DTOs.Product;
using AgriIDMS.Application.Interfaces;
using AgriIDMS.Application.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AgriIDMS.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly IProductService _service;

        public ProductsController(IProductService service)
        {
            _service = service;
        }

        //public ProductsController(IProductService service)
        //{
        //    _service = service;
        //}

        [HttpPost]
        public async Task<IActionResult> Create(CreateProductRequest request)
        {
            var id = await _service.CreateAsync(request);

            return Ok(new
            {
                message = "Tạo sản phẩm thành công",
                id
            });
        }

        [HttpGet]
        public async Task<IActionResult> GetProducts()
        {
            var products = await _service.GetAllProducts();
            return Ok(products);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var data = await _service.GetByIdAsync(id);

            return Ok(data);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, UpdateProductRequest request)
        {
            await _service.UpdateAsync(id, request);

            return Ok(new { message = "Cập nhật thành công" });
        }

        [HttpPatch("{id}")]
        public async Task<IActionResult> UpdateStatus(int id, UpdateProductStatusRequest request)
        {
            await _service.UpdateStatusAsync(id, request);

            return Ok(new { message = "Chuyển trạng thái thành công" });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            await _service.DeleteAsync(id);

            return Ok(new { message = "Xóa thành công" });
        }
    }
}
