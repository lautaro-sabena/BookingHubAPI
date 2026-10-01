using Microsoft.Extensions.Configuration;

namespace BookingHubAPI.API.Configuration;

/// <summary>
/// Resolves the configured CORS origin list, supporting both an indexed array
/// (appsettings.json, or Cors__AllowedOrigins__0/__1/... env vars) and a single flat scalar
/// value carrying a comma-separated list (e.g. one Cors__AllowedOrigins env var, as used by
/// Render and docker-compose). Extracted as a pure function so both bindings are unit-testable
/// without booting a host.
/// </summary>
public static class CorsOriginsResolver
{
    public static string[] Resolve(IConfiguration configuration)
    {
        var origins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();

        if (origins != null && origins.Length > 0)
        {
            return origins;
        }

        var singleValue = configuration["Cors:AllowedOrigins"];
        return string.IsNullOrEmpty(singleValue)
            ? Array.Empty<string>()
            : singleValue.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }
}
