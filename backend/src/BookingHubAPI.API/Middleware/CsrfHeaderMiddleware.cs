using BookingHubAPI.API.Authentication;
using Microsoft.AspNetCore.Mvc;

namespace BookingHubAPI.API.Middleware;

/// <summary>
/// CSRF defence for the cookie session. A state-changing request (POST/PUT/PATCH/DELETE) must send
/// <c>X-Requested-With: BookingHub</c> when it is authenticated by the session cookie, or when it targets an action
/// marked <see cref="RequireCsrfHeaderAttribute"/>. Requests with an <c>Authorization: Bearer</c> header are exempt:
/// a browser never attaches that header on its own, so they cannot be forged cross-site.
/// </summary>
/// <remarks>
/// A custom header is enough here. A cross-site page cannot add one without a CORS preflight, and the CORS
/// whitelist only allows the configured frontend origin; SameSite=Lax on the cookie is the second layer. A
/// double-submit token would add server-side state and a token-refresh flow for no extra protection in this
/// same-origin setup. Must run after routing (it reads endpoint metadata) and before authentication.
/// </remarks>
public class CsrfHeaderMiddleware
{
    private readonly RequestDelegate _next;
    private readonly SessionCookie _sessionCookie;
    private readonly IProblemDetailsService _problemDetailsService;

    public CsrfHeaderMiddleware(RequestDelegate next, SessionCookie sessionCookie, IProblemDetailsService problemDetailsService)
    {
        _next = next;
        _sessionCookie = sessionCookie;
        _problemDetailsService = problemDetailsService;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!IsSatisfied(context))
        {
            await RejectAsync(context);
            return;
        }

        await _next(context);
    }

    private bool IsSatisfied(HttpContext context)
    {
        var request = context.Request;

        if (HttpMethods.IsGet(request.Method) || HttpMethods.IsHead(request.Method)
            || HttpMethods.IsOptions(request.Method) || HttpMethods.IsTrace(request.Method))
        {
            return true;
        }

        if (request.Headers.Authorization.ToString().StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var issuesOrUsesCookie = _sessionCookie.Read(request) is not null
            || context.GetEndpoint()?.Metadata.GetMetadata<RequireCsrfHeaderAttribute>() is not null;

        return !issuesOrUsesCookie
            || string.Equals(request.Headers[CsrfProtection.HeaderName], CsrfProtection.HeaderValue, StringComparison.Ordinal);
    }

    private async Task RejectAsync(HttpContext context)
    {
        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status403Forbidden,
            Detail = $"Requests that change state must send the '{CsrfProtection.HeaderName}: {CsrfProtection.HeaderValue}' header."
        };
        context.Response.StatusCode = StatusCodes.Status403Forbidden;

        var written = await _problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            ProblemDetails = problem
        });

        if (!written)
        {
            await context.Response.WriteAsJsonAsync(problem, (System.Text.Json.JsonSerializerOptions?)null, "application/problem+json");
        }
    }
}
