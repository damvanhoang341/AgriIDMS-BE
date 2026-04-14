using System.Net;
using System.Security.Claims;
using AgriIDMS.Application.DTOs.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AgriIDMS.Application.Services;

namespace AgriIDMS.API.Controllers;

[ApiController]
[Route("api/[controller]/[action]")]
public class AuthController : ControllerBase
{
    private readonly ILogger<AuthController> _logger;
    private readonly AuthService _authService;
    private readonly IConfiguration _config;
    public AuthController(
        ILogger<AuthController> logger,
        AuthService authService,
        IConfiguration config)
    {
        _logger = logger;
        _authService = authService;
        _config = config;
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
        var clientUrl = (_config["AppSettings:ClientUrl"] ?? string.Empty).TrimEnd('/');
        var loginUrl = "https://agreeable-pebble-0c3796b00.7.azurestaticapps.net/login";

        try
        {
            await _authService.ConfirmEmailAsync(userId, token);
            return Content(BuildConfirmEmailHtml("Xác nhận email thành công.", true, loginUrl), "text/html; charset=utf-8");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Confirm email failed for user {UserId}", userId);
            return Content(BuildConfirmEmailHtml(ex.Message, false, loginUrl), "text/html; charset=utf-8");
        }
    }

    [HttpPost("register")]
    [AllowAnonymous]
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
    [AllowAnonymous]
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

    private static string BuildConfirmEmailHtml(string message, bool isSuccess, string loginUrl)
    {
        var safeMessage = WebUtility.HtmlEncode(message);
        var statusTitle = isSuccess ? "Xac nhan email thanh cong" : "Xac nhan email that bai";
        var statusColor = isSuccess ? "#1E8449" : "#C0392B";
        var statusIcon = isSuccess ? "✓" : "!";
        var helperText = isSuccess
            ? "Ban se duoc chuyen den trang dang nhap sau 3 giay."
            : "Lien ket co the da het han hoac khong hop le. Ban se duoc chuyen den trang dang nhap sau 5 giay.";
        var redirectDelay = isSuccess ? 3 : 5;

        return $@"<!doctype html>
<html lang=""vi"">
<head>
  <meta charset=""utf-8"" />
  <meta name=""viewport"" content=""width=device-width, initial-scale=1"" />
  <title>{statusTitle}</title>
  <meta http-equiv=""refresh"" content=""{redirectDelay};url={loginUrl}"" />
  <style>
    body {{
      margin: 0;
      font-family: Arial, sans-serif;
      background: linear-gradient(135deg, #eef7ff 0%, #f8fbff 100%);
      min-height: 100vh;
      display: flex;
      justify-content: center;
      align-items: center;
      color: #1f2937;
    }}
    .card {{
      width: min(560px, 92vw);
      background: #fff;
      border-radius: 16px;
      box-shadow: 0 8px 30px rgba(15, 23, 42, 0.12);
      padding: 28px 24px;
      text-align: center;
    }}
    .icon {{
      width: 54px;
      height: 54px;
      border-radius: 50%;
      margin: 0 auto 16px;
      display: grid;
      place-items: center;
      font-size: 28px;
      font-weight: 700;
      color: #fff;
      background: {statusColor};
    }}
    h1 {{
      margin: 0 0 10px;
      font-size: 22px;
      color: {statusColor};
    }}
    p {{
      margin: 8px 0;
      line-height: 1.5;
    }}
    .btn {{
      display: inline-block;
      margin-top: 16px;
      padding: 10px 18px;
      border-radius: 10px;
      color: #fff;
      background: #2563eb;
      text-decoration: none;
      font-weight: 600;
    }}
  </style>
</head>
<body>
  <main class=""card"">
    <div class=""icon"">{statusIcon}</div>
    <h1>{statusTitle}</h1>
    <p>{safeMessage}</p>
    <p>{helperText}</p>
    <a class=""btn"" href=""{loginUrl}"">Di den trang dang nhap</a>
  </main>
</body>
</html>";
    }
}
