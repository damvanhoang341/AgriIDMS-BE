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
        public async Task<IActionResult> GetAllAsync()
        {
            var data= await _homePageService.GetAllAsync();
            return Ok(data);
        }
    }
}
