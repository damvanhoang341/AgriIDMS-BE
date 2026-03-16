using AgriIDMS.Application.DTOs.ProductVariant;
using AgriIDMS.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace AgriIDMS.API.Controllers
{
    /// <summary>API trang chủ: hiển thị sản phẩm theo luồng Category → Product → ProductVariant (công khai, không cần đăng nhập).</summary>
    [Route("api/[controller]")]
    [ApiController]
    [AllowAnonymous]
    public class HomeController : ControllerBase
    {
        private readonly IHomePageService _homePageService;

        public HomeController(IHomePageService homePageService)
        {
            _homePageService = homePageService;
        }


        [HttpGet]
        public async Task<IActionResult> GetDetailAsync(int id)
        {
            var data= await _homePageService.GetDetailAsync(id);
            return Ok(data);
        }

        [HttpGet("product-variants")]
        public async Task<IActionResult> GetAllProductVariantAsync()
        {
            var data = await _homePageService.GetAllProductVariantAsync();
            return Ok(data);
        }
    }
}
