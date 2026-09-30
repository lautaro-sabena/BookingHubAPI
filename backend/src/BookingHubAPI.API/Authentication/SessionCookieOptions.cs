namespace BookingHubAPI.API.Authentication;

/// <summary>Settings of the httpOnly cookie that carries the session JWT (configuration section <c>Auth:Cookie</c>).</summary>
public sealed class SessionCookieOptions
{
    public const string SectionName = "Auth:Cookie";

    public string Name { get; set; } = "bookinghub_session";

    /// <summary>
    /// Marks the cookie <c>Secure</c> (HTTPS only). On by default; switched off in Development only, so the cookie
    /// also works over plain <c>http://localhost</c> (Safari drops Secure cookies there).
    /// </summary>
    public bool Secure { get; set; } = true;
}
