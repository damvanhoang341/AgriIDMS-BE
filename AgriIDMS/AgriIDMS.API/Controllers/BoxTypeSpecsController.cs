using AgriIDMS.Application.DTOs.BoxTypeSpec;
using AgriIDMS.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgriIDMS.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class BoxTypeSpecsController : ControllerBase
    {
        private readonly IBoxTypeSpecService _boxTypeSpecService;

        public BoxTypeSpecsController(IBoxTypeSpecService boxTypeSpecService)
        {
            _boxTypeSpecService = boxTypeSpecService;
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> GetAll()
        {
            var data = await _boxTypeSpecService.GetAllAsync();
            return Ok(data);
        }

        [HttpPut("replace-all")]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> ReplaceAll([FromBody] List<UpsertBoxTypeSpecItemRequest> items)
        {
            var data = await _boxTypeSpecService.ReplaceAllAsync(items);
            return Ok(new { message = "Lưu cấu hình loại box thành công.", items = data });
        }
    }
}

