using System.Net;
using AspNetCoreRateLimit;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
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
            response.StatusCode.Should().Be(HttpStatusCode.OK,
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
        using var factory = new SingleRequestLimitApiFactory(trustAllProxies: true);
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

    /// <summary>
    /// With ForwardedHeaders:TrustAllProxies off (the default), a client-supplied
    /// X-Forwarded-For must be ignored; otherwise clients could rotate the header to evade limits.
    /// </summary>
    [Fact]
    public async Task ForwardedFor_ShouldBeIgnored_WhenProxiesAreNotTrusted()
    {
        using var factory = new SingleRequestLimitApiFactory(trustAllProxies: false);
        var client = factory.CreateClient();

        var firstRequest = new HttpRequestMessage(HttpMethod.Get, UnmappedProbePath);
        firstRequest.Headers.Add("X-Forwarded-For", "203.0.113.10");
        (await client.SendAsync(firstRequest)).StatusCode.Should().NotBe((HttpStatusCode)429);

        var spoofedRequest = new HttpRequestMessage(HttpMethod.Get, UnmappedProbePath);
        spoofedRequest.Headers.Add("X-Forwarded-For", "198.51.100.20");
        (await client.SendAsync(spoofedRequest)).StatusCode.Should().Be((HttpStatusCode)429,
            "an untrusted X-Forwarded-For must not give the client a fresh counter");
    }

    /// <summary>
    /// The browser talks to the Next.js frontend, which proxies /api to this API through Render's edge again, so
    /// X-Forwarded-For arrives as "client, Next.js egress IP". With ForwardedHeaders:ForwardLimit = 2 (render.yaml)
    /// the limiter keys on the client, not on the Next.js server every user shares.
    /// </summary>
    [Fact]
    public async Task ThroughTheFrontendProxyHop_ShouldKeyTheLimiterOnTheClientIp_WhenForwardLimitIsTwo()
    {
        using var factory = new SingleRequestLimitApiFactory(trustAllProxies: true, forwardLimit: 2);
        var client = factory.CreateClient();

        (await SendWithForwardedFor(client, "203.0.113.10, 192.0.2.99")).StatusCode.Should().NotBe((HttpStatusCode)429);
        (await SendWithForwardedFor(client, "203.0.113.10, 192.0.2.99")).StatusCode.Should().Be((HttpStatusCode)429,
            "the same client behind the same frontend hop exhausts its own counter");
        (await SendWithForwardedFor(client, "198.51.100.20, 192.0.2.99")).StatusCode.Should().NotBe((HttpStatusCode)429,
            "another client behind the same frontend hop must not share that counter");
    }

    /// <summary>The counterpart that justifies the setting: with one trusted hop the shared frontend IP is the key.</summary>
    [Fact]
    public async Task ThroughTheFrontendProxyHop_ShouldPoolAllClients_WhenForwardLimitIsOne()
    {
        using var factory = new SingleRequestLimitApiFactory(trustAllProxies: true, forwardLimit: 1);
        var client = factory.CreateClient();

        (await SendWithForwardedFor(client, "203.0.113.10, 192.0.2.99")).StatusCode.Should().NotBe((HttpStatusCode)429);
        (await SendWithForwardedFor(client, "198.51.100.20, 192.0.2.99")).StatusCode.Should().Be((HttpStatusCode)429,
            "ForwardLimit 1 stops at the frontend hop, which is the same for every user");
    }

    /// <summary>A client-supplied leading entry sits left of the trusted hops and must not choose the key.</summary>
    [Fact]
    public async Task ThroughTheFrontendProxyHop_ShouldIgnoreEntriesTheClientPrepended()
    {
        using var factory = new SingleRequestLimitApiFactory(trustAllProxies: true, forwardLimit: 2);
        var client = factory.CreateClient();

        (await SendWithForwardedFor(client, "1.1.1.1, 203.0.113.10, 192.0.2.99")).StatusCode.Should().NotBe((HttpStatusCode)429);
        (await SendWithForwardedFor(client, "2.2.2.2, 203.0.113.10, 192.0.2.99")).StatusCode.Should().Be((HttpStatusCode)429,
            "rotating a spoofed leading entry must not give the client a fresh counter");
    }

    private static Task<HttpResponseMessage> SendWithForwardedFor(HttpClient client, string forwardedFor)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, UnmappedProbePath);
        request.Headers.Add("X-Forwarded-For", forwardedFor);
        return client.SendAsync(request);
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
    /// Allows one request per client IP and sets ForwardedHeaders:TrustAllProxies explicitly,
    /// so tests can compare the trusted (Render) and untrusted (default) proxy configurations.
    /// </summary>
    private class SingleRequestLimitApiFactory(bool trustAllProxies, int forwardLimit = 1) : BookingApiFactory
    {
        protected override bool RelaxRateLimiting => false;

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ForwardedHeaders:TrustAllProxies"] = trustAllProxies.ToString(),
                    ["ForwardedHeaders:ForwardLimit"] = forwardLimit.ToString()
                });
            });

            // TestServer leaves RemoteIpAddress null, and ForwardedHeadersMiddleware skips its
            // known-proxy check for a null peer. Simulate a real (non-loopback) proxy peer so
            // the trust setting is what decides whether X-Forwarded-For is honored.
            builder.ConfigureServices(services =>
                services.AddTransient<IStartupFilter, SimulatedProxyPeerStartupFilter>());

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

    private sealed class SimulatedProxyPeerStartupFilter : IStartupFilter
    {
        private static readonly System.Net.IPAddress ProxyAddress = System.Net.IPAddress.Parse("10.0.0.1");

        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
        {
            app.Use((context, nextMiddleware) =>
            {
                context.Connection.RemoteIpAddress = ProxyAddress;
                return nextMiddleware(context);
            });
            next(app);
        };
    }
}
