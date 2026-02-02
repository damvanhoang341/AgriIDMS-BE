using AgriIDMS.Application.DTOs.Auth;
using AgriIDMS.Application.Exceptions;
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
}
