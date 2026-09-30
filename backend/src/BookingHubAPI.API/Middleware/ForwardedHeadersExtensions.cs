using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.HttpOverrides;

namespace BookingHubAPI.API.Middleware;

/// <summary>
/// Restores the client IP from <c>X-Forwarded-For</c> with a per-request hop count.
/// <para>
/// Browser traffic reaches the API two ways:
/// <list type="bullet">
/// <item>directly: <c>client -> Render edge -> API</c>, so the chain is <c>[spoofed...,] client</c> and one hop
/// (the edge, which appends the real peer) is trusted;</item>
/// <item>through the frontend's <c>/api</c> proxy: <c>client -> edge -> Next.js -> edge -> API</c>, so the chain is
/// <c>client, Next.js egress IP</c> and the shared Next.js hop must be skipped as well, otherwise the rate limiter
/// would key on one IP for every user. Next.js does not append to the header, it only sets it when absent.</item>
/// </list>
/// A client calling the API directly can put anything in the header, so trusting the extra hop for everybody would
/// let it prepend a fake entry and evade per-IP limits. The extra hop is therefore only trusted when the request
/// proves it went through the frontend by carrying the shared secret <c>FRONTEND_PROXY_KEY</c> in
/// <see cref="FrontendProxyKey.HeaderName"/>. Without a configured key the extra hop is never trusted.
/// </para>
/// </summary>
public static class ForwardedHeadersExtensions
{
    /// <summary>Must run before anything that reads <c>RemoteIpAddress</c> (rate limiting, CORS, auth, audit code).</summary>
    public static IApplicationBuilder UseForwardedClientIp(this IApplicationBuilder app, IConfiguration configuration)
    {
        var trustAllProxies = configuration.GetValue<bool>("ForwardedHeaders:TrustAllProxies");
        var forwardLimit = configuration.GetValue("ForwardedHeaders:ForwardLimit", 1);
        var frontendHops = configuration.GetValue("ForwardedHeaders:FrontendHops", 1);

        // Verifies the key, then removes the header so nothing downstream (or a logger) can see the secret.
        app.Use(new FrontendProxyKey(configuration["FRONTEND_PROXY_KEY"]).MarkAndStrip);

        var direct = CreateOptions(trustAllProxies, forwardLimit);
        var viaFrontend = CreateOptions(trustAllProxies, forwardLimit + frontendHops);
        app.UseWhen(context => !FrontendProxyKey.IsVerified(context), branch => branch.UseForwardedHeaders(direct));
        app.UseWhen(FrontendProxyKey.IsVerified, branch => branch.UseForwardedHeaders(viaFrontend));
        return app;
    }

    private static ForwardedHeadersOptions CreateOptions(bool trustAllProxies, int forwardLimit)
    {
        var options = new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
            ForwardLimit = forwardLimit
        };

        // Render publishes no fixed proxy range, so ForwardedHeaders:TrustAllProxies (render.yaml) trusts the
        // immediate peer. Only enable it where the proxy is the sole ingress; otherwise clients can spoof the header.
        if (trustAllProxies)
        {
            options.KnownNetworks.Clear();
            options.KnownProxies.Clear();
        }

        return options;
    }
}

/// <summary>Constant-time check of the secret the frontend proxy adds to the requests it forwards.</summary>
public sealed class FrontendProxyKey
{
    public const string HeaderName = "X-Frontend-Proxy-Key";
    private static readonly object VerifiedItem = new();

    private readonly byte[]? _expectedHash;

    public FrontendProxyKey(string? key)
    {
        _expectedHash = string.IsNullOrEmpty(key) ? null : Hash(key);
    }

    public static bool IsVerified(HttpContext context) => context.Items.ContainsKey(VerifiedItem);

    public Task MarkAndStrip(HttpContext context, RequestDelegate next)
    {
        var supplied = context.Request.Headers[HeaderName].ToString();
        context.Request.Headers.Remove(HeaderName);

        // Hashing both sides gives FixedTimeEquals equal-length inputs, so neither the value nor its length leaks.
        if (_expectedHash is not null && supplied.Length > 0
            && CryptographicOperations.FixedTimeEquals(Hash(supplied), _expectedHash))
        {
            context.Items[VerifiedItem] = true;
        }

        return next(context);
    }

    private static byte[] Hash(string value) => SHA256.HashData(Encoding.UTF8.GetBytes(value));
}
