using System.Security.Claims;

namespace BookingHubAPI.API.Extensions;

public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// Returns the authenticated user's id from the <see cref="ClaimTypes.NameIdentifier"/> claim.
    /// Throws <see cref="InvalidUserIdentityException"/> (answered as 401 by the error-handling
    /// middleware) when the claim is missing or is not a valid GUID.
    /// </summary>
    public static Guid GetUserId(this ClaimsPrincipal user)
    {
        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId)
            ? userId
            : throw new InvalidUserIdentityException();
    }
}

/// <summary>
/// The authenticated principal carries no usable user id, so the token cannot identify a caller.
/// </summary>
public sealed class InvalidUserIdentityException : Exception
{
    public InvalidUserIdentityException()
        : base("The authenticated principal has a missing or invalid user id claim.")
    {
    }
}
