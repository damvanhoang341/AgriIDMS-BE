using AgriIDMS.Application.DTOs.Auth;
using AgriIDMS.Domain.Entities;
using AgriIDMS.Domain.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;

namespace AgriIDMS.Application.Services;

public class AuthService(IAuthRepository authRepo,
                        UserManager<ApplicationUser> userManager,
                        SignInManager<ApplicationUser> signInManager,
                        IRefreshTokenRepository refreshRepo,
                        ITokenGenerator tokenGen,
                        IUnitOfWork uow,
                        IConfiguration config)
{
    public async Task<AuthResponseDto> LoginAsync(LoginRequestDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.UserNameOrEmail) || string.IsNullOrWhiteSpace(dto.Password))
            throw new ArgumentException("Thiếu tài khoản hoặc mật khẩu.");

        var user = await userManager.FindByNameAsync(dto.UserNameOrEmail)
               ?? await userManager.FindByEmailAsync(dto.UserNameOrEmail);

        if (user == null)
            throw new InvalidOperationException("Sai tài khoản hoặc mật khẩu.");

        var result = await signInManager.PasswordSignInAsync(
            user,
            dto.Password,
            isPersistent: false,
            lockoutOnFailure: true
        );

        if (result.IsLockedOut)
            throw new InvalidOperationException("Tài khoản đang bị khóa tạm thời.");

        if (!result.Succeeded)
            throw new InvalidOperationException("Sai tài khoản hoặc mật khẩu.");

        var roles = await userManager.GetRolesAsync(user);

        // Generate token
        var access = tokenGen.GenerateAccessToken(user.Id, user.UserName!, roles);
        var refresh = tokenGen.GenerateRefreshToken();

        var refreshDays = int.Parse(config["Jwt:RefreshTokenDays"] ?? "14");
        var expires = DateTime.UtcNow.AddDays(refreshDays);

        await refreshRepo.AddAsync(new RefreshToken(refresh, user.Id, expires));
        await uow.SaveChangesAsync();

        // Response
        return new AuthResponseDto
        {
            AccessToken = access,
            RefreshToken = refresh,
            UserId = user.Id,
            UserName = user.UserName!,
            Roles = roles
        };

    }

    public async Task<AuthResponseDto> RefreshAsync(RefreshRequestDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.RefreshToken))
            throw new ArgumentException("Thiếu refresh token.");

        var oldToken = await refreshRepo.GetByTokenAsync(dto.RefreshToken);
        if (oldToken is null || !oldToken.IsActive)
            throw new InvalidOperationException("Refresh token không hợp lệ hoặc đã hết hạn.");

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
}
