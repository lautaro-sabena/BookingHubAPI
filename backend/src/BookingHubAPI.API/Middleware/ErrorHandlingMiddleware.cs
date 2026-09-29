using BookingHubAPI.API.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace BookingHubAPI.API.Middleware;

/// <summary>
/// Turns exceptions that escape the pipeline into RFC 7807 problem responses: 401 when the
/// token has no usable user id, otherwise a generic 500. Exception messages and stack traces
/// are only logged, never sent to clients.
/// </summary>
/// <remarks>
/// A custom middleware is used instead of <c>UseExceptionHandler</c> because the built-in one
/// clears the response headers, which would drop the CORS headers UseCors has already applied
/// (see ErrorHandlingCorsTests).
/// </remarks>
public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger;
    private readonly IProblemDetailsService _problemDetailsService;

    public ErrorHandlingMiddleware(
        RequestDelegate next,
        ILogger<ErrorHandlingMiddleware> logger,
        IProblemDetailsService problemDetailsService)
    {
        _next = next;
        _logger = logger;
        _problemDetailsService = problemDetailsService;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex) when (!context.Response.HasStarted)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var problem = exception is InvalidUserIdentityException
            ? new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Detail = "The access token does not identify a valid user."
            }
            : new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Detail = "An unexpected error occurred."
            };

        if (problem.Status == StatusCodes.Status500InternalServerError)
            _logger.LogError(exception, "An unhandled exception occurred");
        else
            _logger.LogWarning(exception, "Rejected request with an invalid user identity");

        context.Response.StatusCode = problem.Status!.Value;

        // CORS headers are already applied (or withheld) by UseCors against the configured
        // whitelist; setting them here would bypass it.
        await _problemDetailsService.WriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            ProblemDetails = problem
        });
    }
}
