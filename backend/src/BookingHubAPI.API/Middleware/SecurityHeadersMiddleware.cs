namespace BookingHubAPI.API.Middleware;

/// <summary>
/// Adds the response headers every API answer should carry. Headers are written in
/// <c>OnStarting</c>, so they are also present on error bodies, status-code pages and CORS preflights
/// that short-circuit later in the pipeline.
/// </summary>
public static class SecurityHeadersExtensions
{
    // The API only returns JSON: nothing on it needs to load scripts, styles, frames or images, so the CSP is
    // fully closed. There is no Swagger UI in this app (no Swashbuckle/OpenAPI UI registered); if one is added
    // later, relax the CSP for its path only.
    // Cross-Origin-Resource-Policy is deliberately omitted: the frontend and the API are different sites on Render
    // (*.onrender.com is a public suffix), so "same-site" would block legitimate CORS fetches from the frontend.
    private static readonly (string Name, string Value)[] Headers =
    {
        ("X-Content-Type-Options", "nosniff"),
        ("X-Frame-Options", "DENY"),
        ("Referrer-Policy", "no-referrer"),
        ("Content-Security-Policy", "default-src 'none'; frame-ancestors 'none'"),
    };

    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app) =>
        app.Use((context, next) =>
        {
            context.Response.OnStarting(() =>
            {
                foreach (var (name, value) in Headers)
                {
                    context.Response.Headers[name] = value;
                }
                return Task.CompletedTask;
            });
            return next();
        });
}
