using AgriIDMS.Application.DTOs.Auth;
using AgriIDMS.Domain.Entities;
using AgriIDMS.Domain.Interfaces;
using Microsoft.Extensions.Configuration;

namespace AgriIDMS.Application.Services;

public class AuthService(IAuthRepository authRepo,
                        IRefreshTokenRepository refreshRepo,
                        ITokenGenerator tokenGen,
                        IUnitOfWork uow,
                        IConfiguration config)
{
    public async Task<AuthResponseDto> LoginAsync(LoginRequestDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.UserNameOrEmail) || string.IsNullOrWhiteSpace(dto.Password))
            throw new ArgumentException("Thiếu tài khoản hoặc mật khẩu.");

        var (ok, userId, userName) = await authRepo.ValidateUserAsync(dto.UserNameOrEmail, dto.Password);
        if (!ok) throw new InvalidOperationException("Sai tài khoản hoặc mật khẩu.");

        var roles = await authRepo.GetRolesAsync(userId);

        var access = tokenGen.GenerateAccessToken(userId, userName, roles);
        var refresh = tokenGen.GenerateRefreshToken();

        var refreshDays = int.Parse(config["Jwt:RefreshTokenDays"] ?? "14");
        var expires = DateTime.UtcNow.AddDays(refreshDays);

        await refreshRepo.AddAsync(new RefreshToken(refresh, userId, expires));
        await uow.SaveChangesAsync();

        return new AuthResponseDto(access, refresh, userId, userName, roles);
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

        return new AuthResponseDto(access, newRefresh, userId, userName, roles);
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
