using FluentAssertions;

namespace BookingHubAPI.IntegrationTests.Support;

/// <summary>A parsed <c>Set-Cookie</c> header of the session cookie.</summary>
public sealed record SetCookie(string Name, string Value, IReadOnlyList<string> Attributes)
{
    public bool HasAttribute(string name) =>
        Attributes.Any(a => a.Equals(name, StringComparison.OrdinalIgnoreCase));

    public string? AttributeValue(string name) =>
        Attributes.Select(a => a.Split('=', 2))
            .Where(p => p.Length == 2 && p[0].Equals(name, StringComparison.OrdinalIgnoreCase))
            .Select(p => p[1])
            .FirstOrDefault();

    /// <summary>True when the browser would drop the cookie: empty value or an <c>expires</c> in the past.</summary>
    public bool IsExpired => Value.Length == 0
        || DateTimeOffset.TryParse(AttributeValue("expires"), out var expires) && expires <= DateTimeOffset.UtcNow;
}

/// <summary>
/// The API authenticates browsers with an httpOnly cookie and no longer returns the JWT in a body. The test
/// <see cref="HttpClient"/> has no cookie jar, so tests read the token from <c>Set-Cookie</c> and send it explicitly.
/// </summary>
public static class SessionCookies
{
    public const string Name = "bookinghub_session";

    /// <summary>The session cookie set by the response, or null when it sets none.</summary>
    public static SetCookie? SessionCookie(this HttpResponseMessage response)
    {
        if (!response.Headers.TryGetValues("Set-Cookie", out var values))
        {
            return null;
        }

        foreach (var header in values)
        {
            var parts = header.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            var pair = parts[0].Split('=', 2);
            if (pair[0] == Name)
            {
                return new SetCookie(pair[0], pair.Length > 1 ? pair[1] : string.Empty, parts.Skip(1).ToList());
            }
        }

        return null;
    }

    /// <summary>The JWT carried by the session cookie of the response.</summary>
    public static string SessionToken(this HttpResponseMessage response)
    {
        var cookie = response.SessionCookie();
        cookie.Should().NotBeNull("the response must set the session cookie");
        return cookie!.Value;
    }

    /// <summary>Sends the token the way a browser sends the cookie.</summary>
    public static HttpRequestMessage WithSessionCookie(this HttpRequestMessage request, string token)
    {
        request.Headers.Add("Cookie", $"{Name}={token}");
        return request;
    }
}
