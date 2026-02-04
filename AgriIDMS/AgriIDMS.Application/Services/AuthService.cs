using AgriIDMS.Application.DTOs.Auth;
using AgriIDMS.Application.Exceptions;
using AgriIDMS.Domain.Exceptions;
using AgriIDMS.Domain.Entities;
using AgriIDMS.Domain.Enums;
using AgriIDMS.Domain.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AgriIDMS.Application.Services;

public class AuthService(IAuthRepository authRepo,
                        UserManager<ApplicationUser> userManager,
                        IRefreshTokenRepository refreshRepo,
                        ITokenGenerator tokenGen,
                        IUnitOfWork uow,
                        ILogger<AuthService> logger,
                        IEmailService emailService,
                        IConfiguration config)
{
    public async Task<AuthResponseDto> LoginAsync(LoginRequestDto dto)
    {
        var user = await userManager.FindByNameAsync(dto.UserNameOrEmail)
            ?? await userManager.FindByEmailAsync(dto.UserNameOrEmail);

        if (user == null)
            throw new UnauthorizedException("Sai tài khoản hoặc mật khẩu.");

        // 🔒 Check lockout trước
        if (await userManager.IsLockedOutAsync(user))
            throw new LockedException("Tài khoản đang bị khóa tạm thời.");

        // 🔑 Check password
        var validPassword = await userManager.CheckPasswordAsync(user, dto.Password);

        if (!validPassword)
        {
            await userManager.AccessFailedAsync(user);

            if (await userManager.IsLockedOutAsync(user))
                throw new LockedException("Tài khoản đang bị khóa tạm thời.");

            throw new UnauthorizedException("Sai tài khoản hoặc mật khẩu.");
        }

        // ✅ Login đúng → reset fail count
        await userManager.ResetAccessFailedCountAsync(user);

        var roles = await userManager.GetRolesAsync(user);

        // 🔐 Generate JWT
        var accessToken = tokenGen.GenerateAccessToken(user.Id, user.UserName!, roles);
        var refreshToken = tokenGen.GenerateRefreshToken();

        var refreshDays = int.Parse(config["Jwt:RefreshTokenDays"] ?? "14");
        var expires = DateTime.UtcNow.AddDays(refreshDays);

        await refreshRepo.AddAsync(
            new RefreshToken(refreshToken, user.Id, expires)
        );

        await uow.SaveChangesAsync();

        return new AuthResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            UserId = user.Id,
            UserName = user.UserName!,
            Roles = roles
        };
    }


    public async Task<AuthResponseDto> RefreshAsync(RefreshRequestDto dto)
    {

        var oldToken = await refreshRepo.GetByTokenAsync(dto.RefreshToken);
        if (oldToken is null || !oldToken.IsActive)
            throw new UnauthorizedException("Refresh token không hợp lệ hoặc đã hết hạn.");

        // revoke token cũ
        oldToken.Revoke();
        refreshRepo.Update(oldToken);

        var userId = oldToken.UserId;
        var userName = await authRepo.GetUserNameAsync(userId);
        var roles = await authRepo.GetRolesAsync(userId);

        var access = tokenGen.GenerateAccessToken(userId, userName, roles);

        var newRefresh = tokenGen.GenerateRefreshToken();
        var refreshDays = int.Parse(config["Jwt:RefreshTokenDays"] ?? "14");
        var expires = DateTime.UtcNow.AddDays(refreshDays);

        await refreshRepo.AddAsync(new RefreshToken(newRefresh, userId, expires));
        await uow.SaveChangesAsync();

        return new AuthResponseDto
        {
            AccessToken = access,
            RefreshToken = newRefresh,
            UserId = userId,
            UserName = userName,
            Roles = roles
        };
    }

    public async Task LogoutAsync(string userId, LogoutRequestDto dto)
    {
        if (string.IsNullOrWhiteSpace(userId)) return;
        if (string.IsNullOrWhiteSpace(dto.RefreshToken)) return;

        var token = await refreshRepo.GetByTokenAsync(dto.RefreshToken);
        if (token is null) return;
        if (token.UserId != userId) return;

        token.Revoke();
        refreshRepo.Update(token);
        await uow.SaveChangesAsync();
    }

    public async Task ConfirmEmailAsync(Guid userId, string token)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        if (user == null)
            throw new InvalidBusinessRuleException("User không tồn tại");

        var result = await userManager.ConfirmEmailAsync(user, token);

        if (!result.Succeeded)
        {
            var errors = string.Join(" | ", result.Errors.Select(e => e.Description));
            throw new InvalidBusinessRuleException(errors);
        }
    }



    private string BuildVerifyEmailBody(
    string email,
    string userId,
    string emailConfirmToken,
    string password)
    {
        var confirmLink =
            $"{config["AppSettings:ClientUrl"]}/api/v1/Auth/ConfirmEmail/confirm-email" +
            $"?userId={userId}&token={Uri.EscapeDataString(emailConfirmToken)}";

        return $@"
<p>Xin chào,</p>

<p>Tài khoản nhân viên của bạn đã được tạo thành công. 🎉</p>

<p>
<b>Thông tin đăng nhập:</b><br/>
- Email: {email}<br/>
- Mật khẩu tạm thời: <b>{password}</b>
</p>

<p>
Vui lòng xác nhận email tại đây:<br/>
<a href='{confirmLink}'>{confirmLink}</a>
</p>

<p>Sau khi đăng nhập lần đầu, hãy đổi mật khẩu ngay.</p>

<p>Trân trọng,<br/>Hệ thống</p>
";
    }

    private void SendVerifyEmailInBackground(string email, string body)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await emailService.SendAsync(
                    email,
                    "Tài khoản nhân viên & xác nhận email",
                    body
                );
            }
            catch (Exception ex)
            {
                // TODO: log ex
            }
        });
    }



    public async Task CreateEmployeeAsync(RegisterEmployeeDto request)
    {
        if (!request.Email.EndsWith("@gmail.com"))
            throw new InvalidBusinessRuleException("Email không thuộc công ty");

        if (await userManager.FindByEmailAsync(request.Email) != null)
            throw new InvalidBusinessRuleException("Email đã tồn tại");

        var randomPassword = $"Aa1!{Guid.NewGuid():N}".Substring(0, 12);

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            EmailConfirmed = false
        };

        user.SetUserType(UserType.Employee);
        user.SetRegisterMethod(RegisterMethod.AdminCreated);

        var result = await userManager.CreateAsync(user, randomPassword);
        if (!result.Succeeded)
            throw new InvalidBusinessRuleException("Tạo nhân viên thất bại");

        await userManager.AddToRoleAsync(user, request.Role);
        var emailToken = await userManager.GenerateEmailConfirmationTokenAsync(user);
        var emailBody = BuildVerifyEmailBody(
        user.Email!,
        user.Id,
        emailToken,
        randomPassword
        );
        SendVerifyEmailInBackground(user.Email!, emailBody);
    }

    public async Task RegisterCustomerAsync(RegisterCustomerRequest request)
    {
        if (!string.IsNullOrWhiteSpace(request.UserName))
        {
            var existedByUserName = await userManager.FindByNameAsync(request.UserName);
            if (existedByUserName != null)
                throw new InvalidBusinessRuleException("UserName đã tồn tại");
        }

        var user = new ApplicationUser
        {
            UserName = request.UserName,
            Email = $"{request.UserName}@system.local",
            EmailConfirmed = true 
        };

        user.SetUserType(UserType.Customer);
        user.SetRegisterMethod(RegisterMethod.Username);

        var result = await userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(x => x.Description));
            throw new InvalidBusinessRuleException(
                $"Đăng ký khách hàng thất bại: {errors}"
            );
        }

        await userManager.AddToRoleAsync(user, "Customer");
    }

    public async Task ForgotPasswordAndResetAsync(ForgotPasswordRequest dto)
    {
        var user = await userManager.FindByEmailAsync(dto.Email);

        if (user == null || !user.EmailConfirmed)
            throw new NotFoundException("Không tìm thấy user với email đã cho");

        var newPassword = $"Aa1!{Guid.NewGuid():N}".Substring(0, 12);

        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var result = await userManager.ResetPasswordAsync(user, token, newPassword);

        if (!result.Succeeded)
            throw new ApplicationException("Reset password thất bại");

        // 🔥 GỬI MAIL BACKGROUND
        _ = Task.Run(async () =>
        {
            try
            {
                await emailService.SendAsync(
                    user.Email!,
                    "Mật khẩu mới của bạn",
                    $@"
                <p>Hệ thống đã reset mật khẩu cho bạn.</p>
                <p><b>Mật khẩu mới:</b> {newPassword}</p>
                <p>Vui lòng đăng nhập và đổi mật khẩu ngay.</p>
                "
                );
            }
            catch (Exception ex)
            {
                // TODO: log lỗi
            }
        });
    }

}
