using System.Security.Claims;
using BookingHubAPI.API.Extensions;
using Xunit;

namespace BookingHubAPI.IntegrationTests.Extensions;

public class ClaimsPrincipalExtensionsTests
{
    private static ClaimsPrincipal PrincipalWith(params Claim[] claims) =>
        new(new ClaimsIdentity(claims, "test"));

    [Fact]
    public void GetUserId_ShouldReturnGuid_FromNameIdentifierClaim()
    {
        var id = Guid.NewGuid();
        var principal = PrincipalWith(new Claim(ClaimTypes.NameIdentifier, id.ToString()));

        Assert.Equal(id, principal.GetUserId());
    }

    [Fact]
    public void GetUserId_ShouldThrowInvalidUserIdentityException_WhenClaimIsMissing()
    {
        var principal = PrincipalWith(new Claim(ClaimTypes.Email, "user@test.com"));

        Assert.Throws<InvalidUserIdentityException>(() => principal.GetUserId());
    }

    [Fact]
    public void GetUserId_ShouldThrowInvalidUserIdentityException_WhenClaimIsNotAGuid()
    {
        var principal = PrincipalWith(new Claim(ClaimTypes.NameIdentifier, "not-a-guid"));

        Assert.Throws<InvalidUserIdentityException>(() => principal.GetUserId());
    }
}
