using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace BookingHubAPI.IntegrationTests;

/// <summary>
/// Security headers are present on every response: successes, error bodies (404 problem+json,
/// 401) and CORS preflights. HSTS is only emitted outside Development, over HTTPS, and never for
/// localhost, so it is asserted through a Production-environment factory and a non-local host.
/// </summary>
public class SecurityHeadersTests : IClassFixture<BookingApiFactory>
{
    private readonly BookingApiFactory _factory;

    public SecurityHeadersTests(BookingApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task SuccessResponse_ShouldCarrySecurityHeaders()
    {
        var response = await _factory.CreateClient().GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        AssertSecurityHeaders(response);
    }

    [Fact]
    public async Task NotFoundProblem_ShouldCarrySecurityHeaders()
    {
        var response = await _factory.CreateClient().GetAsync("/api/does-not-exist");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        AssertSecurityHeaders(response);
    }

    [Fact]
    public async Task Unauthorized_ShouldCarrySecurityHeaders()
    {
        var response = await _factory.CreateClient().GetAsync("/api/favorites");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        AssertSecurityHeaders(response);
    }

    [Fact]
    public async Task CorsPreflight_ShouldCarrySecurityHeaders()
    {
        var request = new HttpRequestMessage(HttpMethod.Options, "/api/favorites");
        request.Headers.Add("Origin", "http://localhost:3000");
        request.Headers.Add("Access-Control-Request-Method", "GET");

        var response = await _factory.CreateClient().SendAsync(request);

        // Prove the CORS middleware actually answered the preflight before checking the headers.
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        Header(response, "Access-Control-Allow-Origin").Should().Be("http://localhost:3000");
        AssertSecurityHeaders(response);
    }

    [Fact]
    public async Task App_ShouldNotAddAServerHeader()
    {
        var response = await _factory.CreateClient().GetAsync("/health");

        // TestServer has no Kestrel, so this only guards against the app adding the header itself;
        // Kestrel's AddServerHeader = false is configured in Program.cs.
        response.Headers.Contains("Server").Should().BeFalse();
    }

    [Fact]
    public async Task Development_ShouldNotSendHsts()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://api.example.com")
        });

        var response = await client.GetAsync("/health");

        response.Headers.Contains("Strict-Transport-Security").Should().BeFalse();
    }

    [Fact]
    public async Task Production_ShouldSendHstsOverHttps()
    {
        using var factory = new ProductionApiFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://api.example.com")
        });

        var response = await client.GetAsync("/health");

        response.Headers.GetValues("Strict-Transport-Security").Single().Should().Contain("max-age=");
        AssertSecurityHeaders(response);
    }

    private static void AssertSecurityHeaders(HttpResponseMessage response)
    {
        Header(response, "X-Content-Type-Options").Should().Be("nosniff");
        Header(response, "X-Frame-Options").Should().Be("DENY");
        Header(response, "Referrer-Policy").Should().Be("no-referrer");
        Header(response, "Content-Security-Policy").Should().Be("default-src 'none'; frame-ancestors 'none'");
        response.Headers.Contains("Server").Should().BeFalse();
    }

    private static string Header(HttpResponseMessage response, string name)
    {
        response.Headers.TryGetValues(name, out var values).Should().BeTrue($"{name} must be present");
        return values!.Single();
    }

    private class ProductionApiFactory : BookingApiFactory
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);
            builder.UseEnvironment("Production");
        }
    }
}
