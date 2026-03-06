using AgriIDMS.Application.DTOs.User;
using AgriIDMS.Application.Interfaces;
using AgriIDMS.Application.Pagination;
using AgriIDMS.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AgriIDMS.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;

        public UsersController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpGet]
        public async Task<IActionResult> GetUsers(
            [FromQuery] int pageIndex = 1,
            [FromQuery] int pageSize = 10)
        {
            var result = await _userService.GetPagedAsync(
                new PaginationRequest
                {
                    PageIndex = pageIndex,
                    PageSize = pageSize
                });

            return Ok(result);
        }

        [HttpDelete("{userId:guid}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteUser([FromRoute] string userId)
        {
            await _userService.DeleteAsync(userId);
            return Ok("xóa thành công");
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetUserById(string id)
        {
            var result = await _userService.GetUserByIdAsync(id);

            if (result == null)
                return NotFound("User không tồn tại.");

            return Ok(result);
        }

        [HttpGet("my-profile")]
        [Authorize]
        public async Task<IActionResult> GetMyProfile()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var profile = await _userService.GetUserByIdAsync(userId);

            return Ok(profile);
        }

        [HttpPut("profile/{id}")]
        [Authorize]
        public async Task<IActionResult> UpdateProfile(string id,[FromBody] UpdateUserProfileDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();
            await _userService.UpdateProfileAsync(userId, dto);

            return Ok("Cập nhật thành công");
        }
    }
}
