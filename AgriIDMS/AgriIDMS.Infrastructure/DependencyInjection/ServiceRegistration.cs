using System.Security.Claims;
using System.Text;
using AgriIDMS.Application.Interfaces;
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
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IGoodsReceiptRepository, GoodsReceiptRepository>();
        services.AddScoped<IGoodsReceiptDetailRepository, GoodsReceiptDetailRepository>();
        services.AddScoped<ILotRepository, LotRepository>();
        services.AddScoped<ISupplierRepository, SupplierRepository>();
        services.AddScoped<IWarehouseRepository, WarehouseRepository>();
        services.AddScoped<IZoneRepository, ZoneRepository>();
        services.AddScoped<IRackRepository, RackRepository>();
        services.AddScoped<ISlotRepository, SlotRepository>();
        services.AddScoped<IProductVariantRepository, ProductVariantRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IPurchaseOrderRepository, PurchaseOrderRepository>();
        services.AddScoped<IBoxRepository, BoxRepository>();
        services.AddScoped<IInventoryTransactionRepository, InventoryTransactionRepository>();

        // Application use-case service (implementation ở Application)
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<ISupplierService, SupplierService>();
        services.AddScoped<IUserService,UserService>();
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<IProductVariantService, ProductVariantService>();
        services.AddScoped<IGoodsReceiptService,GoodsReceiptService>();
        services.AddScoped<IPurchaseOrderService, PurchaseOrderService>();
        services.AddScoped<AuthService>();
        services.AddScoped<IWarehouseService, WarehouseService>();
        services.AddScoped<IZoneService, ZoneService>();
        services.AddScoped<IRackService, RackService>();
        services.AddScoped<ISlotService, SlotService>();



        return services;
    }
}
