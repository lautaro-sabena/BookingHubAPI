using BookingHubAPI.API.Configuration;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace BookingHubAPI.IntegrationTests.Configuration;

public class CorsOriginsResolverTests
{
    [Fact]
    public void Resolve_WithIndexedArrayConfig_ShouldReturnAllOrigins()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Cors:AllowedOrigins:0"] = "http://localhost:3000",
                ["Cors:AllowedOrigins:1"] = "http://localhost:5173"
            })
            .Build();

        CorsOriginsResolver.Resolve(configuration).Should().BeEquivalentTo(
            new[] { "http://localhost:3000", "http://localhost:5173" });
    }

    [Fact]
    public void Resolve_WithCommaSeparatedSingleValue_ShouldSplitIntoMultipleOrigins()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Cors:AllowedOrigins"] = "http://localhost:3000, http://localhost:5173"
            })
            .Build();

        CorsOriginsResolver.Resolve(configuration).Should().BeEquivalentTo(
            new[] { "http://localhost:3000", "http://localhost:5173" });
    }

    [Fact]
    public void Resolve_WithSingleScalarValue_ShouldReturnOneOrigin()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Cors:AllowedOrigins"] = "https://bookinghubapi-frontend-h0ui.onrender.com"
            })
            .Build();

        CorsOriginsResolver.Resolve(configuration).Should().ContainSingle()
            .Which.Should().Be("https://bookinghubapi-frontend-h0ui.onrender.com");
    }

    [Fact]
    public void Resolve_WhenMissing_ShouldReturnEmptyArray()
    {
        var configuration = new ConfigurationBuilder().Build();

        CorsOriginsResolver.Resolve(configuration).Should().BeEmpty();
    }
}
