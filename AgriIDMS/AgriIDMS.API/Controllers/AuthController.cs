// BaseApp.API/Controllers/AuthController.cs
using System.Security.Claims;
using AgriIDMS.Application.Services;
using AgriIDMS.Application.DTOs.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace BaseApp.API.Controllers;

[ApiController]
[Route("api/v1/[controller]/[action]")]
public class AuthController : ControllerBase
{
    private readonly ILogger<AuthController> _logger;
    private readonly AuthService _authService;

    public AuthController(ILogger<AuthController> logger, AuthService authService)
    {
        _logger = logger;
        _authService = authService;
    }

    /// <summary>
    /// Login -> trả AccessToken + RefreshToken
    /// </summary>
    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        try
        {
            var data = await _authService.LoginAsync(dto);
            return Ok(data);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Login failed");
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Refresh -> đổi AccessToken mới + RefreshToken mới
    /// </summary>
    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequestDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        try
        {
            var data = await _authService.RefreshAsync(dto);
            return Ok(data);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Refresh failed");
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Logout -> revoke refresh token (cần Bearer token)
    /// </summary>
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Logout([FromBody] LogoutRequestDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var userId = User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

        await _authService.LogoutAsync(userId, dto);
        return Ok(new { message = "Logged out." });
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("admin/create-employee")]
    //[HttpPost]
    public async Task<IActionResult> CreateEmployee([FromBody] RegisterEmployeeDto request)
    {
        await _authService.CreateEmployeeAsync(request);
        return Ok("Tạo nhân viên thành công");
    }

    /// <summary>
    /// Xác nhận email người dùng
    /// </summary>
    [HttpGet("confirm-email")]
    [AllowAnonymous]
    public async Task<IActionResult> ConfirmEmail([FromQuery] Guid userId, [FromQuery] string token)
    {
        var x = 1;
        await _authService.ConfirmEmailAsync(userId, token);

        return Ok(new { message = "Xác nhận email thành công" });
    }

    [HttpPost("register")]
    public async Task<IActionResult> RegisterCustomer(
        [FromBody] RegisterCustomerRequest request)
    {
        await _authService.RegisterCustomerAsync(request);

        return Ok(new
        {
            message = "Đăng ký khách hàng thành công"
        });
    }

    /// <summary>
    /// User quên mật khẩu → hệ thống reset và gửi mật khẩu mới qua email
    /// </summary>
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword(
        [FromBody] ForgotPasswordRequest request)
    {
        await _authService.ForgotPasswordAndResetAsync(request);

        return Ok(new
        {
            message = "Mật khẩu mới đã được gửi về email nếu tài khoản tồn tại"
        });
    }

    /// <summary>
    /// Đổi mật khẩu khi đã đăng nhập
    /// </summary>
    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword(
        [FromBody] ChangePasswordRequest request)
    {
        await _authService.ChangePasswordAsync(request);

        return Ok(new
        {
            message = "Đổi mật khẩu thành công"
        });
    }

}
