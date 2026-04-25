using AgriIDMS.API.Middleware;
using Microsoft.AspNetCore.Mvc;
using AgriIDMS.Infrastructure.Data;
using AgriIDMS.Infrastructure.DependencyInjection;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Controllers
builder.Services.AddControllers();
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var errors = context.ModelState
            .Where(x => x.Value?.Errors.Count > 0)
            .ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value!.Errors.Select(e => e.ErrorMessage).ToArray()
            );

        var firstError = errors.SelectMany(x => x.Value).FirstOrDefault()
                         ?? "Dữ liệu gửi lên không hợp lệ.";

        return new BadRequestObjectResult(new
        {
            message = firstError,
            errors
        });
    };
});

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReact",policy =>
        {
            policy.WithOrigins(
                    "http://localhost:5173",
                    "https://agreeable-pebble-0c3796b00.7.azurestaticapps.net",
                    "https://agreeable-pebble-0e3796b00.7.azurestaticapps.net"
                )
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        });
});


// DI Infrastructure (Identity + EF + JWT + Services)
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// Áp dụng migration còn thiếu. Lỗi ở đây thường thành 500.0 từ IIS/ANCM (chưa vào GlobalExceptionMiddleware);
// log rõ ràng giúp xem nguyên nhân trong App Service / Application Insights.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var startupLogger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Startup.Database");
    try
    {
        var pending = db.Database.GetPendingMigrations().ToList();
        if (pending.Count > 0)
            startupLogger.LogInformation("Áp dụng {Count} migration: {Migrations}", pending.Count, string.Join(", ", pending));
        db.Database.Migrate();
    }
    catch (Exception ex)
    {
        startupLogger.LogCritical(
            ex,
            "Database.Migrate() thất bại. Kiểm tra connection string, tường lửa Azure SQL, quyền (DDL), và tính tương thích migration với DB hiện tại.");
        throw;
    }
}

app.UseMiddleware<GlobalExceptionMiddleware>();

// Always enable Swagger (for testing / local + Azure)
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "AgriIDMS API V1");
});

// Seed Identity
//using (var scope = app.Services.CreateScope())
//{
//    try
//    {
//        await IdentitySeeder.SeedAsync(scope.ServiceProvider);
//    }
//    catch (Exception ex)
//    {
//        Console.WriteLine("Seed failed: " + ex);
//        throw; 
//    }
//}

app.UseHttpsRedirection();

app.UseCors("AllowReact");
app.UseStatusCodePages();

// JWT
app.UseAuthentication();
app.UseAuthorization();

// Map controllers
app.MapControllers();

app.Run();
