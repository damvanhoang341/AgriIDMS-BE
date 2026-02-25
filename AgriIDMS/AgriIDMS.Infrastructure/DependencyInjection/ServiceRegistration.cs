using System.Security.Claims;
using System.Text;
using AgriIDMS.Application.Services;
using AgriIDMS.Domain.Entities;
using AgriIDMS.Domain.Interfaces;
using AgriIDMS.Infrastructure.Data;
using AgriIDMS.Infrastructure.Repositories;
using AgriIDMS.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace AgriIDMS.Infrastructure.DependencyInjection;

public static class ServiceRegistration
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        // DbContext
        services.AddDbContext<AppDbContext>(opt =>
            opt.UseSqlServer(config.GetConnectionString("DefaultConnection")));

        // Identity (đầy đủ Cookie + SignInManager + Lockout)
        services.AddIdentity<ApplicationUser, IdentityRole>(options =>
        {
            // Email
            options.SignIn.RequireConfirmedEmail = true;
            options.User.RequireUniqueEmail = true;

            // Password
            options.Password.RequiredLength = 8;
            options.Password.RequireNonAlphanumeric = false;

            // Lockout
            options.Lockout.AllowedForNewUsers = true;
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(10);
        })
        .AddEntityFrameworkStores<AppDbContext>()
        .AddDefaultTokenProviders();

        var issuer = config["Jwt:Issuer"]!;
        var audience = config["Jwt:Audience"]!;
        var key = config["Jwt:Key"]!;

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
            .AddJwtBearer(opt =>
            {
                opt.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = issuer,

                    ValidateAudience = true,
                    ValidAudience = audience,

                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(key)),

                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30)
                };
            });

        services.AddAuthorization();

        // Domain abstractions -> Infrastructure implementations
        services.AddScoped<IAuthRepository, AuthRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<ITokenGenerator, TokenGenerator>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<UserService>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<ProductService>();
        services.AddScoped<IGoodsReceiptRepository, GoodsReceiptRepository>();

        // Application use-case service (implementation ở Application)
        services.AddScoped<AuthService>();

        return services;
    }
}
