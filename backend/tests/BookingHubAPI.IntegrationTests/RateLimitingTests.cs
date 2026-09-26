using System.Net;
using AspNetCoreRateLimit;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace BookingHubAPI.IntegrationTests;

/// <summary>
/// Proves that rate-limit rules are actually loaded into configuration (Program.cs used to
/// bind IpRateLimitOptions from a section that only ever existed in a file the host never
/// read, so every rule was silently ignored) and that the limiter enforces it end to end.
/// </summary>
public class RateLimitingTests
{
    /// <summary>
    /// An unmapped path (no controller/health-check route matches it). AspNetCoreRateLimit's
    /// middleware runs ahead of routing and inspects the raw request path/method, so it still
    /// counts and blocks requests here even though an allowed request eventually falls through
    /// to a 404 - which keeps this test independent of any real endpoint's whitelisting.
    /// </summary>
    private const string UnmappedProbePath = "/api/__test/rate-limit-probe";

    private const int TinyLimit = 3;

    [Fact]
    public void ProductionConfiguration_ShouldLoadRulesFromRateLimitJsonFile()
    {
        using var factory = new ProductionRulesApiFactory();

        var options = factory.Services.GetRequiredService<IOptions<IpRateLimitOptions>>().Value;

        options.GeneralRules.Should().NotBeNullOrEmpty(
            "IpRateLimiting.GeneralRules must be loaded into configuration for rules to bind");
        options.GeneralRules.Should().Contain(r => r.Endpoint == "POST:/api/auth/login" && r.Limit == 5);
        options.GeneralRules.Should().Contain(r => r.Endpoint == "POST:/api/auth/register" && r.Limit == 3);
    }

    [Fact]
    public async Task ExceedingConfiguredLimit_ShouldReturnTooManyRequests()
    {
        using var factory = new TinyRateLimitApiFactory();
        var client = factory.CreateClient();

        HttpResponseMessage? lastResponse = null;
        for (var i = 0; i < TinyLimit + 1; i++)
        {
            lastResponse = await client.GetAsync(UnmappedProbePath);
        }

        lastResponse.Should().NotBeNull();
        lastResponse!.StatusCode.Should().Be((HttpStatusCode)429);
    }

    [Fact]
    public async Task HealthEndpoint_ShouldBeExcludedFromRateLimiting()
    {
        using var factory = new TinyRateLimitApiFactory();
        var client = factory.CreateClient();

        for (var i = 0; i < TinyLimit + 5; i++)
        {
            var response = await client.GetAsync("/health");
            response.StatusCode.Should().NotBe((HttpStatusCode)429,
                "/health is on IpRateLimiting.EndpointWhitelist and must never be throttled");
        }
    }

    /// <summary>
    /// Behind Render's proxy every client's connection IP is the proxy's own IP, so the
    /// limiter must key on the forwarded client IP (via ForwardedHeadersMiddleware, enabled
    /// through ForwardedHeaders:TrustAllProxies) rather than the shared connection IP -
    /// otherwise two unrelated clients could share, or evict each other from, one counter.
    /// </summary>
    [Fact]
    public async Task DifferentForwardedForValues_ShouldGetIndependentRateLimitCounters()
    {
        using var factory = new TrustedProxyTinyRateLimitApiFactory();
        var client = factory.CreateClient();

        var requestFromFirstIp = new HttpRequestMessage(HttpMethod.Get, UnmappedProbePath);
        requestFromFirstIp.Headers.Add("X-Forwarded-For", "203.0.113.10");
        var firstResponse = await client.SendAsync(requestFromFirstIp);
        firstResponse.StatusCode.Should().NotBe((HttpStatusCode)429,
            "the first request from this forwarded IP is within its own limit");

        var requestExhaustingFirstIp = new HttpRequestMessage(HttpMethod.Get, UnmappedProbePath);
        requestExhaustingFirstIp.Headers.Add("X-Forwarded-For", "203.0.113.10");
        var exhaustedResponse = await client.SendAsync(requestExhaustingFirstIp);
        exhaustedResponse.StatusCode.Should().Be((HttpStatusCode)429,
            "the second request from the same forwarded IP exceeds its tiny per-IP limit");

        var requestFromSecondIp = new HttpRequestMessage(HttpMethod.Get, UnmappedProbePath);
        requestFromSecondIp.Headers.Add("X-Forwarded-For", "198.51.100.20");
        var secondIpResponse = await client.SendAsync(requestFromSecondIp);
        secondIpResponse.StatusCode.Should().NotBe((HttpStatusCode)429,
            "a different forwarded IP must have its own counter, unaffected by the first IP's");
    }

    /// <summary>Does not relax rate limiting, so IpRateLimitOptions reflects appsettings.json as-is.</summary>
    private class ProductionRulesApiFactory : BookingApiFactory
    {
        protected override bool RelaxRateLimiting => false;
    }

    /// <summary>
    /// Overrides the real rules with a tiny limit so the 429 assertion is fast and
    /// deterministic, while still proving the production wiring (config load + full
    /// AspNetCoreRateLimit middleware registration) actually enforces whatever
    /// IpRateLimitOptions resolves to.
    /// </summary>
    private class TinyRateLimitApiFactory : BookingApiFactory
    {
        protected override bool RelaxRateLimiting => false;

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);

            builder.ConfigureServices(services =>
            {
                services.PostConfigure<IpRateLimitOptions>(options =>
                {
                    options.GeneralRules = new List<RateLimitRule>
                    {
                        new() { Endpoint = "*", Period = "1m", Limit = TinyLimit }
                    };
                });
            });
        }
    }

    /// <summary>
    /// Same tiny-limit setup as <see cref="TinyRateLimitApiFactory"/>, plus
    /// ForwardedHeaders:TrustAllProxies so the limiter keys on X-Forwarded-For instead of the
    /// shared TestServer connection IP (mirrors the Render deployment's configuration).
    /// </summary>
    private class TrustedProxyTinyRateLimitApiFactory : BookingApiFactory
    {
        protected override bool RelaxRateLimiting => false;

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ForwardedHeaders:TrustAllProxies"] = "true"
                });
            });

            base.ConfigureWebHost(builder);

            builder.ConfigureServices(services =>
            {
                services.PostConfigure<IpRateLimitOptions>(options =>
                {
                    options.GeneralRules = new List<RateLimitRule>
                    {
                        new() { Endpoint = "*", Period = "1m", Limit = 1 }
                    };
                });
            });
        }
    }
}
