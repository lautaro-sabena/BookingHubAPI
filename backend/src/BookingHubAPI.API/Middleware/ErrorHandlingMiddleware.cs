using System.Net;
using System.Text.Json;

namespace BookingHubAPI.API.Middleware;

public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;

    public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
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
            _logger.LogError(ex, "An unhandled exception occurred");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var code = HttpStatusCode.InternalServerError;
        var result = string.Empty;

        switch (exception)
        {
            case ArgumentException argumentException:
                code = HttpStatusCode.BadRequest;
                result = JsonSerializer.Serialize(new { error = argumentException.Message });
                break;
            case UnauthorizedAccessException:
                code = HttpStatusCode.Unauthorized;
                result = JsonSerializer.Serialize(new { error = "Unauthorized" });
                break;
            case InvalidOperationException:
                code = HttpStatusCode.Conflict;
                result = JsonSerializer.Serialize(new { error = exception.Message });
                break;
            default:
                result = JsonSerializer.Serialize(new { error = "An internal server error occurred" });
                break;
        }

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)code;
        
        var origin = context.Request.Headers["Origin"].ToString();
        if (!string.IsNullOrEmpty(origin))
        {
            context.Response.Headers["Access-Control-Allow-Origin"] = origin;
            context.Response.Headers["Access-Control-Allow-Methods"] = "GET, POST, PUT, DELETE, OPTIONS";
            context.Response.Headers["Access-Control-Allow-Headers"] = "Content-Type, Authorization";
        }

        return context.Response.WriteAsync(result);
    }
}
