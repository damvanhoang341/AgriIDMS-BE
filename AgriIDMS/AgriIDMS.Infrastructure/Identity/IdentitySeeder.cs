using AgriIDMS.Domain.Constants;
using AgriIDMS.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

public static class IdentitySeeder
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        await SeedRolesAsync(serviceProvider);
        await SeedAdminAsync(serviceProvider);
    }

    private static async Task SeedRolesAsync(IServiceProvider serviceProvider)
    {
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        foreach (var role in AppRole.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }
    }

    private static async Task SeedAdminAsync(IServiceProvider serviceProvider)
    {
        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        const string adminUserName = "admin";
        const string adminEmail = "admin@system.local";
        const string adminPassword = "Admin@123";

        var admin = await userManager.FindByNameAsync(adminUserName);
        if (admin != null) return;

        admin = new ApplicationUser
        {
            UserName = adminUserName,
            Email = adminEmail,
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(admin, adminPassword);
        if (!result.Succeeded)
        {
            throw new Exception("Không thể tạo tài khoản Admin: "
                + string.Join(", ", result.Errors.Select(e => e.Description)));
        }

        await userManager.AddToRoleAsync(admin, AppRole.Admin);
    }
}
