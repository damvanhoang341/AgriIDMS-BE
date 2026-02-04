// BaseApp.API/Controllers/AuthController.cs
using System.Security.Claims;
using AgriIDMS.Application.Services;
using AgriIDMS.Application.DTOs.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
    public async Task<IActionResult> CreateEmployee(RegisterEmployeeDto request)
    {
        await _authService.CreateEmployeeAsync(request);
        return Ok("Tạo nhân viên thành công");
    }

}
