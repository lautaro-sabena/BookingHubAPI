using System.Net;
using FluentAssertions;
using Xunit;

namespace BookingHubAPI.IntegrationTests;

/// <summary>
/// Proves that every origin configured as a JSON array in Cors:AllowedOrigins is allowed by
/// the CORS policy - fixing the bug where appsettings.Development.json used to bind this key
/// as a single comma-joined string that never matched a real Origin header, silently
/// rejecting every client in Development.
/// </summary>
public class CorsConfigurationTests : IClassFixture<BookingApiFactory>
{
    private static readonly string[] ConfiguredOrigins = ["http://localhost:3000", "http://localhost:5173"];

    private readonly BookingApiFactory _factory;

    public CorsConfigurationTests(BookingApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ArrayConfiguredOrigins_ShouldAllBeAllowed()
    {
        var client = _factory.CreateClient();

        foreach (var origin in ConfiguredOrigins)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "/health");
            request.Headers.Add("Origin", origin);

            var response = await client.SendAsync(request);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Headers.GetValues("Access-Control-Allow-Origin").Should().ContainSingle(origin,
                $"{origin} is one of two origins configured as an array in appsettings.Development.json");
        }
    }
}
