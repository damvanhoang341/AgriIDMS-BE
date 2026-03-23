using System.Security.Claims;
using System.Text;
using AgriIDMS.Application.Interfaces;
using AgriIDMS.Application.Services;
using AgriIDMS.Domain.Entities;
using AgriIDMS.Domain.Interfaces;
using AgriIDMS.Infrastructure.Data;
using AgriIDMS.Infrastructure.Repositories;
using AgriIDMS.Infrastructure.Services;
using AgriIDMS.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using PayOS;

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
        services.AddScoped<IStockCheckRepository, StockCheckRepository>();
        services.AddScoped<IStockCheckDetailRepository, StockCheckDetailRepository>();
        services.AddScoped<IInventoryRequestRepository, InventoryRequestRepository>();
        services.AddScoped<ICartRepository, CartRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IOrderAllocationRepository, OrderAllocationRepository>();
        services.AddScoped<IComplaintRepository, ComplaintRepository>();
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<IExportReceiptRepository, ExportReceiptRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<IUserNotificationRepository, UserNotificationRepository>();
        //services.AddScoped<ILotRepository, LotRepository>();

        // Application use-case service (implementation ở Application)
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<ISupplierService, SupplierService>();
        services.AddScoped<IUserService,UserService>();
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<IProductVariantService, ProductVariantService>();
        services.AddScoped<IGoodsReceiptService,GoodsReceiptService>();
        services.AddScoped<IGoodsReceiptDetailService, GoodsReceiptDetailService>();
        services.AddScoped<IPurchaseOrderService, PurchaseOrderService>();
        services.AddScoped<AuthService>();
        services.AddScoped<IWarehouseService, WarehouseService>();
        services.AddScoped<IZoneService, ZoneService>();
        services.AddScoped<IRackService, RackService>();
        services.AddScoped<ISlotService, SlotService>();
        services.AddScoped<IBoxService, BoxService>();
        services.AddScoped<IStockCheckService, StockCheckService>();
        services.AddScoped<ICartItemService, CartItemService>();
        services.AddScoped<ICartService, CartService>();
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<IComplaintService, ComplaintService>();
        services.AddScoped<IPaymentService, PaymentService>();
        services.AddScoped<IExportService, ExportService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IHomePageService, HomePageService>();
        services.AddScoped<ILotService, LotService>();

        // Background workers
        services.AddHostedService<BackorderExpiryScannerService>();
        services.AddHostedService<NearExpiryLotScannerService>();

        // Cross-cutting services
        // PayOS client (singleton vì nội bộ dùng HttpClient)
        var payOsSection = config.GetSection("PayOS");
        services.AddSingleton(new PayOSClient(new PayOSOptions
        {
            ClientId = payOsSection["ClientId"]!,
            ApiKey = payOsSection["ApiKey"]!,
            ChecksumKey = payOsSection["ChecksumKey"]!
        }));

        return services;
    }
}
