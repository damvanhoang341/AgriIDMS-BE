namespace AgriIDMS.API.Middleware
{
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;

        public GlobalExceptionMiddleware(RequestDelegate next)
        {
            _next = next;
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

        private static Task HandleExceptionAsync(
            HttpContext context,
            Exception exception)
        {
            context.Response.ContentType = "application/json";

            var statusCode = exception switch
            {
                Application.Exceptions.UnauthorizedException => StatusCodes.Status401Unauthorized,
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

            var message = statusCode == StatusCodes.Status500InternalServerError
                ? "Đã xảy ra lỗi hệ thống"
                : exception.Message;

            // Nếu là lỗi EF, ưu tiên hiển thị inner exception để biết rõ nguyên nhân (FK/unique/constraint...)
            var inner = exception.InnerException?.Message;
            if (isDev && !string.IsNullOrWhiteSpace(inner))
            {
                message = $"{exception.Message} | Inner: {inner}";
            }

            return context.Response.WriteAsJsonAsync(new
            {
                error = message,
                detail = isDev ? exception.ToString() : null
            });
        }
    }

}
