namespace BookingHubAPI.API.Authentication;

/// <summary>The custom request header cookie-authenticated (and cookie-issuing) requests must carry.</summary>
public static class CsrfProtection
{
    public const string HeaderName = "X-Requested-With";
    public const string HeaderValue = "BookingHub";
}

/// <summary>
/// Marks an action that sets or clears the session cookie (login, register, logout). Such an action requires
/// <see cref="CsrfProtection"/> even for anonymous callers, otherwise a foreign site could sign a victim in to an
/// attacker's account ("login CSRF") with a cross-site form post.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class RequireCsrfHeaderAttribute : Attribute
{
}
