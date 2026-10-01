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
        var section = configuration.GetSection("Cors:AllowedOrigins");

        // A scalar set on the key itself (e.g. the Cors__AllowedOrigins env var) must override
        // array entries from appsettings*.json, which stay visible as indexed children.
        if (!string.IsNullOrWhiteSpace(section.Value))
        {
            return section.Value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        }

        return section.Get<string[]>() ?? Array.Empty<string>();
    }
}
