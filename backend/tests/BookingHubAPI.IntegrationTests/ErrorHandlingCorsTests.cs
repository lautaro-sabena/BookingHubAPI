using System.Net;
using BookingHubAPI.IntegrationTests.Support;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BookingHubAPI.IntegrationTests;

/// <summary>
/// Proves that ErrorHandlingMiddleware no longer reflects the request's raw Origin header
/// into Access-Control-Allow-Origin on unhandled-exception responses. Instead, the CORS
/// middleware (which runs earlier in the pipeline, before this middleware's exception is
/// caught) must have already applied - or withheld - headers according to the configured
/// whitelist, and ErrorHandlingMiddleware must not disturb them.
/// </summary>
public class ErrorHandlingCorsTests : IClassFixture<ErrorHandlingCorsTests.ThrowingEndpointApiFactory>
{
    private const string ThrowingPath = "/api/__test/throw";

    private readonly ThrowingEndpointApiFactory _factory;

    public ErrorHandlingCorsTests(ThrowingEndpointApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task UnhandledException_WithDisallowedOrigin_ShouldNotReflectOrigin()
    {
        var client = _factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Get, ThrowingPath);
        request.Headers.Add("Origin", "https://evil.example");

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        response.Headers.Contains("Access-Control-Allow-Origin").Should().BeFalse(
            "a disallowed origin must never receive CORS headers, including on error responses");
    }

    [Fact]
    public async Task UnhandledException_WithAllowedOrigin_ShouldIncludeConfiguredOrigin()
    {
        var configuration = _factory.Services.GetRequiredService<IConfiguration>();
        var configuredOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
        var allowedOrigin = configuredOrigins?.FirstOrDefault(o => !string.IsNullOrWhiteSpace(o));
        allowedOrigin.Should().NotBeNullOrEmpty(
            "the test host must have a configured CORS origin array to exercise, e.g. Cors:AllowedOrigins in appsettings.json");

        var client = _factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Get, ThrowingPath);
        request.Headers.Add("Origin", allowedOrigin);

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        response.Headers.GetValues("Access-Control-Allow-Origin").Should().ContainSingle(allowedOrigin!);
    }

    [Fact]
    public async Task UnhandledException_ShouldReturnGenericProblemWithoutExceptionDetails()
    {
        var response = await _factory.CreateClient().GetAsync(ThrowingPath);

        var problem = await response.ShouldBeProblemAsync(HttpStatusCode.InternalServerError, "An unexpected error occurred.");
        var raw = System.Text.Json.JsonSerializer.Serialize(problem);
        raw.Should().NotContain("Test-only").And.NotContain("ErrorHandlingCorsTests").And.NotContain("StackTrace");
    }

    public class ThrowingEndpointApiFactory : BookingApiFactory
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);

            builder.ConfigureServices(services =>
            {
                services.AddSingleton<IStartupFilter>(new ThrowingEndpointStartupFilter(ThrowingPath));
            });
        }
    }

    private class ThrowingEndpointStartupFilter : IStartupFilter
    {
        private readonly string _path;

        public ThrowingEndpointStartupFilter(string path)
        {
            _path = path;
        }

        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
        {
            return app =>
            {
                next(app);

                // Appended after the real pipeline (Program.cs), so it only runs for
                // requests routing didn't match to a real controller action - it still
                // executes inside ErrorHandlingMiddleware's try/catch and after UseCors,
                // exactly like a genuine unhandled exception from a controller would.
                app.Use(async (context, nextMiddleware) =>
                {
                    if (context.Request.Path == _path)
                    {
                        // A plain Exception falls through to ErrorHandlingMiddleware's
                        // default (500) branch; ArgumentException/UnauthorizedAccessException/
                        // InvalidOperationException are handled as distinct status codes there.
                        throw new Exception(
                            "Test-only exception to exercise ErrorHandlingMiddleware.");
                    }

                    await nextMiddleware(context);
                });
            };
        }
    }
}
