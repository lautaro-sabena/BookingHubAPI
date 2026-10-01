using System.IdentityModel.Tokens.Jwt;
using BookingHubAPI.Infrastructure.Auth;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace BookingHubAPI.UnitTests.Auth;

public class JwtServiceTests
{
    private static JwtService CreateService()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:SecretKey"] = "UnitTestSecretKey_1234567890123456",
                ["Jwt:Issuer"] = "BookingHubAPI",
                ["Jwt:Audience"] = "BookingHubAPI",
                ["Jwt:ExpirationMinutes"] = "60"
            })
            .Build();

        return new JwtService(configuration);
    }

    [Fact]
    public void GenerateToken_WithCompanyId_ShouldIncludeCompanyIdClaim()
    {
        var service = CreateService();
        var companyId = Guid.NewGuid();

        var token = service.GenerateToken(Guid.NewGuid(), "owner@test.com", "Owner", companyId);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        var claim = jwt.Claims.FirstOrDefault(c => c.Type == "companyId");

        claim.Should().NotBeNull();
        claim!.Value.Should().Be(companyId.ToString());
    }

    [Fact]
    public void GenerateToken_WithoutCompanyId_ShouldNotIncludeCompanyIdClaim()
    {
        var service = CreateService();

        var token = service.GenerateToken(Guid.NewGuid(), "customer@test.com", "Customer", companyId: null);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        jwt.Claims.Should().NotContain(c => c.Type == "companyId");
    }
}
