using BookingHubAPI.API.Configuration;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace BookingHubAPI.IntegrationTests.Configuration;

// Lives in IntegrationTests, not UnitTests, because CorsOriginsResolver is in the API project
// and BookingHubAPI.UnitTests only references Domain/Application/Infrastructure - referencing
// API from UnitTests would pull in ASP.NET Core hosting and break that layering boundary.
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

    [Fact]
    public void Resolve_WhenEnvScalarOverridesJsonArray_ShouldPreferTheScalar()
    {
        // Mimics appsettings.Development.json's indexed array being layered under an
        // environment-variable-style flat "Cors:AllowedOrigins" override, exactly as
        // docker-compose and Render set Cors__AllowedOrigins alongside
        // ASPNETCORE_ENVIRONMENT=Development: the flat env value must win even though the
        // JSON provider's indexed children are still present in the composite configuration.
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Cors:AllowedOrigins:0"] = "http://localhost:3000",
                ["Cors:AllowedOrigins:1"] = "http://localhost:5173"
            })
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Cors:AllowedOrigins"] = "https://bookinghubapi-frontend-h0ui.onrender.com"
            })
            .Build();

        CorsOriginsResolver.Resolve(configuration).Should().BeEquivalentTo(
            new[] { "https://bookinghubapi-frontend-h0ui.onrender.com" });
    }
}
