using Microsoft.Extensions.Configuration;

namespace BookingHubAPI.Infrastructure.Configuration;

/// <summary>
/// Reads required-but-secret configuration values and fails fast, at startup, with an
/// actionable message naming the missing key and how to set it - instead of a cryptic
/// null-reference or crypto error the first time the value is actually used.
/// </summary>
public static class StartupConfigurationValidator
{
    public const int MinimumJwtSecretKeyLength = 32;

    public static string RequireJwtSecretKey(IConfiguration configuration)
    {
        var secretKey = configuration["Jwt:SecretKey"]
            ?? throw new InvalidOperationException(
                "Missing required configuration 'Jwt:SecretKey'. In Development, set it with " +
                "dotnet user-secrets set \"Jwt:SecretKey\" \"<a random value of at least " +
                $"{MinimumJwtSecretKeyLength} characters>\" (run from backend/src/BookingHubAPI.API); " +
                "in other environments, set the Jwt__SecretKey environment variable. " +
                "See backend/README.md for details.");

        if (secretKey.Length < MinimumJwtSecretKeyLength)
        {
            throw new InvalidOperationException(
                $"Configuration 'Jwt:SecretKey' must be at least {MinimumJwtSecretKeyLength} characters long " +
                "for HMAC-SHA256 signing security.");
        }

        return secretKey;
    }

    public static string RequireConnectionString(IConfiguration configuration)
    {
        return configuration["ConnectionStrings:DefaultConnection"]
            ?? throw new InvalidOperationException(
                "Missing required configuration 'ConnectionStrings:DefaultConnection'. In Development, set it with " +
                "dotnet user-secrets set \"ConnectionStrings:DefaultConnection\" " +
                "\"Host=localhost;Port=5432;Database=bookinghubdb;Username=postgres;Password=<your-password>\" " +
                "(run from backend/src/BookingHubAPI.API); in other environments, set the " +
                "ConnectionStrings__DefaultConnection environment variable. See backend/README.md for details.");
    }
}
