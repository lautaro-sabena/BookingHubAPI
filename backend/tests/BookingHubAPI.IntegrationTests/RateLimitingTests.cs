using System.Net;
using AspNetCoreRateLimit;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace BookingHubAPI.IntegrationTests;

/// <summary>
/// Proves that rate-limit.json is actually loaded into configuration (Program.cs used to
/// bind IpRateLimitOptions from a section that only ever existed in a file the host never
/// read, so every rule was silently ignored) and that the limiter enforces it end to end.
/// </summary>
public class RateLimitingTests
{
    [Fact]
    public void ProductionConfiguration_ShouldLoadRulesFromRateLimitJsonFile()
    {
        using var factory = new ProductionRulesApiFactory();

        var options = factory.Services.GetRequiredService<IOptions<IpRateLimitOptions>>().Value;

        options.GeneralRules.Should().NotBeNullOrEmpty(
            "rate-limit.json must be loaded into configuration for rules to bind");
        options.GeneralRules.Should().Contain(r => r.Endpoint == "POST:/api/auth/login" && r.Limit == 5);
        options.GeneralRules.Should().Contain(r => r.Endpoint == "POST:/api/auth/register" && r.Limit == 3);
    }

    [Fact]
    public async Task ExceedingConfiguredLimit_ShouldReturnTooManyRequests()
    {
        using var factory = new TinyRateLimitApiFactory();
        var client = factory.CreateClient();

        HttpResponseMessage? lastResponse = null;
        for (var i = 0; i < 4; i++)
        {
            lastResponse = await client.GetAsync("/health");
        }

        lastResponse.Should().NotBeNull();
        lastResponse!.StatusCode.Should().Be((HttpStatusCode)429);
    }

    /// <summary>Does not relax rate limiting, so IpRateLimitOptions reflects rate-limit.json as-is.</summary>
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
                        new() { Endpoint = "*", Period = "1m", Limit = 3 }
                    };
                });
            });
        }
    }
}
