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

        /// <summary>Lấy catalog hiển thị trang chủ: danh mục → sản phẩm → biến thể (chỉ Active, có số lượng tồn).</summary>
        [HttpGet("catalog")]
        public async Task<IActionResult> GetCatalog()
        {
            var result = await _homePageService.GetCatalogForHomePageAsync();
            return Ok(result);
        }
    }
}
