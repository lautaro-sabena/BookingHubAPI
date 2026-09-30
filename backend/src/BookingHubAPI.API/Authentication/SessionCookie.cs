using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;

namespace BookingHubAPI.API.Authentication;

/// <summary>
/// Writes, clears and reads the session cookie. The cookie is <c>HttpOnly</c> (scripts cannot read the JWT),
/// <c>SameSite=Lax</c> and <c>Path=/</c>, and expires together with the JWT it carries.
/// </summary>
public sealed class SessionCookie
{
    private readonly SessionCookieOptions _options;

    public SessionCookie(IOptions<SessionCookieOptions> options)
    {
        _options = options.Value;
    }

    public string Name => _options.Name;

    public string? Read(HttpRequest request) =>
        request.Cookies.TryGetValue(_options.Name, out var value) && !string.IsNullOrEmpty(value) ? value : null;

    public void Append(HttpResponse response, string token)
    {
        var expires = new DateTimeOffset(new JsonWebToken(token).ValidTo, TimeSpan.Zero);
        response.Cookies.Append(_options.Name, token, CreateOptions(expires));
    }

    /// <summary>Expires the cookie; the attributes must match the ones it was set with or browsers keep it.</summary>
    public void Delete(HttpResponse response) =>
        response.Cookies.Delete(_options.Name, CreateOptions(expires: null));

    private CookieOptions CreateOptions(DateTimeOffset? expires) => new()
    {
        HttpOnly = true,
        Secure = _options.Secure,
        SameSite = SameSiteMode.Lax,
        Path = "/",
        IsEssential = true,
        Expires = expires
    };
}
