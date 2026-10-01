using AspNetCoreRateLimit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace BookingHubAPI.API.Middleware;

/// <summary>
/// AspNetCoreRateLimit's IP middleware that answers a throttled request with an RFC 7807
/// <c>application/problem+json</c> 429 (like every other API error) instead of the library's
/// plain-text message. The <c>Retry-After</c> header is kept.
/// </summary>
/// <remarks>
/// It still runs where <c>UseIpRateLimiting</c> did, after UseCors and the security-headers
/// middleware, so those headers are already on the response (see SecurityHeadersTests and
/// ErrorHandlingCorsTests).
/// </remarks>
public class ProblemDetailsRateLimitMiddleware : IpRateLimitMiddleware
{
    private readonly IOptions<IpRateLimitOptions> _options;
    private readonly IProblemDetailsService _problemDetailsService;

    public ProblemDetailsRateLimitMiddleware(
        RequestDelegate next,
        IProcessingStrategy processingStrategy,
        IOptions<IpRateLimitOptions> options,
        IIpPolicyStore policyStore,
        IRateLimitConfiguration config,
        ILogger<IpRateLimitMiddleware> logger,
        IProblemDetailsService problemDetailsService)
        : base(next, processingStrategy, options, policyStore, config, logger)
    {
        _options = options;
        _problemDetailsService = problemDetailsService;
    }

    public override async Task ReturnQuotaExceededResponse(HttpContext httpContext, RateLimitRule rule, string retryAfter)
    {
        var options = _options.Value;

        if (!options.DisableRateLimitHeaders)
        {
            httpContext.Response.Headers["Retry-After"] = retryAfter;
        }

        httpContext.Response.StatusCode = options.QuotaExceededResponse?.StatusCode ?? options.HttpStatusCode;

        var problem = new ProblemDetails
        {
            Status = httpContext.Response.StatusCode,
            Title = "Too Many Requests",
            Detail = $"Too many requests. Retry after {retryAfter} seconds."
        };

        var written = await _problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem
        });

        // No writer accepts the request (e.g. a restrictive Accept header): still answer problem+json.
        if (!written)
        {
            await httpContext.Response.WriteAsJsonAsync(problem, (System.Text.Json.JsonSerializerOptions?)null, "application/problem+json");
        }
    }
}
