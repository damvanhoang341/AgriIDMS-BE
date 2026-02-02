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

            return context.Response.WriteAsJsonAsync(new
            {
                error = exception.Message
            });
        }
    }

}
