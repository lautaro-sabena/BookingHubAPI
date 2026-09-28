using System.Security.Claims;

namespace BookingHubAPI.API.Extensions;

public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// Returns the authenticated user's id from the <see cref="ClaimTypes.NameIdentifier"/> claim.
    /// Throws <see cref="ArgumentNullException"/> when the claim is missing and
    /// <see cref="FormatException"/> when it is not a valid GUID.
    /// </summary>
    public static Guid GetUserId(this ClaimsPrincipal user)
    {
        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.Parse(userIdClaim!);
    }
}
