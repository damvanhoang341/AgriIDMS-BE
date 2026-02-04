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



    private async Task SendVerifyEmailAsync(ApplicationUser user)
    {
        var token = await userManager.GenerateEmailConfirmationTokenAsync(user);

        var confirmLink =
            $"{config["AppSettings:ClientUrl"]}/api/v1/Auth/ConfirmEmail/confirm-email" +
            $"?userId={user.Id}&token={Uri.EscapeDataString(token)}";


        await emailService.SendAsync(
            user.Email!,
            "Xác nhận email",
            $@"
            <p>Vui lòng xác nhận email:</p>
            <a href='{confirmLink}'>Xác nhận</a>
        "
        );
    }


    public async Task CreateEmployeeAsync(RegisterEmployeeDto request)
    {
        if (!request.Email.EndsWith("@gmail.com"))
            throw new InvalidBusinessRuleException("Email không thuộc công ty");

        if (await userManager.FindByEmailAsync(request.Email) != null)
            throw new InvalidBusinessRuleException("Email đã tồn tại");

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            EmailConfirmed = false
        };

        user.SetUserType(UserType.Employee);
        user.SetRegisterMethod(RegisterMethod.AdminCreated);

        var result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
            throw new InvalidBusinessRuleException("Tạo nhân viên thất bại");

        await userManager.AddToRoleAsync(user, request.Role);
        await SendVerifyEmailAsync(user);

    }

    public async Task RegisterCustomerAsync(RegisterCustomerRequest request)
    {
        // Rule 1: Phải có UserName hoặc Email
        if (string.IsNullOrWhiteSpace(request.UserName)
            && string.IsNullOrWhiteSpace(request.Email))
        {
            throw new InvalidBusinessRuleException(
                "Phải nhập UserName hoặc Email"
            );
        }

        // Rule 2: Check trùng UserName (nếu có)
        if (!string.IsNullOrWhiteSpace(request.UserName))
        {
            var existedByUserName = await userManager.FindByNameAsync(request.UserName);
            if (existedByUserName != null)
                throw new InvalidBusinessRuleException("UserName đã tồn tại");
        }

        // Rule 3: Check trùng Email (nếu có)
        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            var existedByEmail = await userManager.FindByEmailAsync(request.Email);
            if (existedByEmail != null)
                throw new InvalidBusinessRuleException("Email đã tồn tại");
        }

        // Tạo user
        var user = new ApplicationUser
        {
            UserName = request.UserName ?? request.Email!, // ưu tiên UserName
            Email = request.Email
        };

        user.SetUserType(UserType.Customer);
        RegisterMethod registerMethod;

        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            registerMethod = RegisterMethod.Email;
        }
        else
        {
            registerMethod = RegisterMethod.Username;
        }

        user.SetRegisterMethod(registerMethod);

        // Create
        var result = await userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(x => x.Description));
            throw new InvalidBusinessRuleException(
                $"Đăng ký khách hàng thất bại: {errors}"
            );
        }

        // Gán role Customer (nếu bạn có role)
        await userManager.AddToRoleAsync(user, "Customer");
    }


}
