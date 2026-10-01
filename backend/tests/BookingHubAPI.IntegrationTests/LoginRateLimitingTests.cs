using System.Net;
using System.Net.Http.Json;
using AspNetCoreRateLimit;
using BookingHubAPI.Application.DTOs;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace BookingHubAPI.IntegrationTests;

/// <summary>
/// Proves the production login throttle (POST:/api/auth/login in appsettings.json's
/// IpRateLimiting.GeneralRules) is enforced end to end against the real auth endpoint, not
/// just bound into configuration: the (limit + 1)th login within the period is rejected
/// with 429. Uses its own factory instance (RelaxRateLimiting = false), independent of every
/// other test class, so it never shares a rate-limit counter with them.
/// </summary>
public class LoginRateLimitingTests
{
    [Fact]
    public async Task Login_ExceedingConfiguredLimit_ShouldReturnTooManyRequests()
    {
        using var factory = new ProductionRulesApiFactory();
        var client = factory.CreateClient();

        var loginLimit = factory.Services.GetRequiredService<IOptions<IpRateLimitOptions>>().Value
            .GeneralRules.Single(r => r.Endpoint == "POST:/api/auth/login").Limit;

        var registerRequest = new RegisterRequest("login-throttle@test.com", "Password123!", "Customer");
        var registerResponse = await client.PostAsJsonAsync("/api/auth/register", registerRequest);
        registerResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var loginRequest = new LoginRequest("login-throttle@test.com", "Password123!");

        for (var attempt = 1; attempt <= loginLimit; attempt++)
        {
            var response = await client.PostAsJsonAsync("/api/auth/login", loginRequest);
            response.StatusCode.Should().Be(HttpStatusCode.OK,
                $"attempt {attempt} of {loginLimit} is within the configured login limit");
        }

        var throttledResponse = await client.PostAsJsonAsync("/api/auth/login", loginRequest);
        throttledResponse.StatusCode.Should().Be((HttpStatusCode)429,
            $"the ({loginLimit} + 1)th login attempt within the period must be throttled");
    }

    /// <summary>Does not relax rate limiting, so IpRateLimitOptions reflects appsettings.json as-is.</summary>
    private class ProductionRulesApiFactory : BookingApiFactory
    {
        protected override bool RelaxRateLimiting => false;
    }
}
