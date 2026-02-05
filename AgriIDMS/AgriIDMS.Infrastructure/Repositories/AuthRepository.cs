// AgriIDMS.Infrastructure/Repositories/AuthRepository.cs
using AgriIDMS.Domain.Entities;
using AgriIDMS.Domain.Interfaces;
using Microsoft.AspNetCore.Identity;

namespace AgriIDMS.Infrastructure.Repositories;

public class AuthRepository(UserManager<ApplicationUser> userManager) : IAuthRepository
{

    public async Task<IList<string>> GetRolesAsync(string userId)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null) return new List<string>();
        return await userManager.GetRolesAsync(user);
    }

    public async Task<string> GetUserNameAsync(string userId)
    {
        var user = await userManager.FindByIdAsync(userId);
        return user?.UserName ?? "";
    }
}
