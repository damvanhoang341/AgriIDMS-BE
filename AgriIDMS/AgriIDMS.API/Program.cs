using AgriIDMS.API.Middleware;
using AgriIDMS.Infrastructure.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Controllers
builder.Services.AddControllers();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReact",policy =>
        {
            policy.WithOrigins("http://localhost:5173",
                "https://<your-fe>.azurestaticapps.net")
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        });
});


// DI Infrastructure (Identity + EF + JWT + Services)
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

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
