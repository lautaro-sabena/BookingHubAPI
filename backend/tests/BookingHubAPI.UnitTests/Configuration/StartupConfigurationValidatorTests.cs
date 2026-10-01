using BookingHubAPI.Infrastructure.Configuration;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace BookingHubAPI.UnitTests.Configuration;

public class StartupConfigurationValidatorTests
{
    [Fact]
    public void RequireJwtSecretKey_WhenMissing_ShouldThrowWithActionableMessage()
    {
        var configuration = new ConfigurationBuilder().Build();

        var act = () => StartupConfigurationValidator.RequireJwtSecretKey(configuration);

        act.Should().Throw<InvalidOperationException>()
            .Which.Message.Should().Contain("Jwt:SecretKey").And.Contain("user-secrets set");
    }

    [Fact]
    public void RequireJwtSecretKey_WhenShorterThanMinimumLength_ShouldThrow()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Jwt:SecretKey"] = "tooShort" })
            .Build();

        var act = () => StartupConfigurationValidator.RequireJwtSecretKey(configuration);

        act.Should().Throw<InvalidOperationException>()
            .Which.Message.Should().Contain(StartupConfigurationValidator.MinimumJwtSecretKeyLength.ToString());
    }

    [Fact]
    public void RequireJwtSecretKey_WhenValid_ShouldReturnIt()
    {
        const string validKey = "UnitTestValidSecretKey1234567890";
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Jwt:SecretKey"] = validKey })
            .Build();

        StartupConfigurationValidator.RequireJwtSecretKey(configuration).Should().Be(validKey);
    }

    [Fact]
    public void RequireConnectionString_WhenMissing_ShouldThrowWithActionableMessage()
    {
        var configuration = new ConfigurationBuilder().Build();

        var act = () => StartupConfigurationValidator.RequireConnectionString(configuration);

        act.Should().Throw<InvalidOperationException>()
            .Which.Message.Should().Contain("ConnectionStrings:DefaultConnection").And.Contain("user-secrets set");
    }

    [Fact]
    public void RequireConnectionString_WhenPresent_ShouldReturnIt()
    {
        const string connectionString = "Host=localhost;Port=5432;Database=bookinghubdb;Username=postgres;Password=x";
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = connectionString
            })
            .Build();

        StartupConfigurationValidator.RequireConnectionString(configuration).Should().Be(connectionString);
    }
}
