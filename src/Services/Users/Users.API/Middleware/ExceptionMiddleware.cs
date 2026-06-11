using System.Net;
using Users.Domain.Exceptions;

namespace Users.API.Middleware;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;
    private readonly IHostEnvironment _env;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger, IHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (DomainException ex)
        {
            _logger.LogWarning("Domain rule violation: {Message}", ex.Message);

            context.Response.StatusCode = ex.StatusCode;
            context.Response.ContentType = "application/json";

            await context.Response.WriteAsJsonAsync(new
            {
                type   = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                title  = ex.StatusCode == 409 ? "Conflict" : "Bad Request",
                status = ex.StatusCode,
                detail = ex.Message
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");

            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            context.Response.ContentType = "application/json";

            await context.Response.WriteAsJsonAsync(new
            {
                type   = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
                title  = "Internal Server Error",
                status = 500,
                detail = _env.IsDevelopment() ? ex.Message : "An unexpected error occurred."
            });
        }
    }
}
