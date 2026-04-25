using Microsoft.Data.SqlClient;

namespace AgriIDMS.API.Middleware
{
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;

        public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(
            HttpContext context,
            Exception exception)
        {
            context.Response.ContentType = "application/json";

            var statusCode = exception switch
            {
                Application.Exceptions.UnauthorizedException => StatusCodes.Status401Unauthorized,
                UnauthorizedAccessException => StatusCodes.Status401Unauthorized,
                Application.Exceptions.LockedException => StatusCodes.Status423Locked,
                Application.Exceptions.NotFoundException => StatusCodes.Status404NotFound,
                Application.Exceptions.ConflictException => StatusCodes.Status409Conflict,
                Application.Exceptions.ForbiddenException => StatusCodes.Status403Forbidden,
                Domain.Exceptions.DomainException => StatusCodes.Status400BadRequest,
                _ => StatusCodes.Status500InternalServerError
            };

            context.Response.StatusCode = statusCode;
            var isDev = string.Equals(
                Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
                "Development",
                StringComparison.OrdinalIgnoreCase);

            if (statusCode == StatusCodes.Status500InternalServerError)
            {
                // Luôn ghi log đầy đủ (Azure App Service / Application Insights) — client prod vẫn chỉ nhận message chung.
                var baseEx = exception.GetBaseException();
                int? sqlNumber = baseEx is SqlException sql0 ? sql0.Number : (int?)null;
                _logger.LogError(
                    exception,
                    "Lỗi 500: {Path} | {ExType} | SqlErrorNumber: {SqlNumber} | BaseMessage: {BaseMessage}",
                    context.Request.Path,
                    exception.GetType().Name,
                    sqlNumber,
                    baseEx.Message);
            }

            var message = statusCode == StatusCodes.Status500InternalServerError
                ? "Đã xảy ra lỗi hệ thống"
                : exception.Message;

            // Trong môi trường Dev: luôn hiển thị message thật để debug nhanh 500.
            if (isDev)
            {
                message = exception.Message;
            }

            // Nếu là lỗi EF, ưu tiên hiển thị inner exception để biết rõ nguyên nhân (FK/unique/constraint...)
            var inner = exception.InnerException?.Message;
            if (isDev && !string.IsNullOrWhiteSpace(inner))
            {
                message = $"{exception.Message} | Inner: {inner}";
            }

            await context.Response.WriteAsJsonAsync(new
            {
                message = message,
                error = message,
                detail = isDev ? exception.ToString() : null
            });
        }
    }

}
