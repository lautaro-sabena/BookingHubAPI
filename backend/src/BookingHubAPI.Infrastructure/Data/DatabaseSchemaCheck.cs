using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Logging;

namespace BookingHubAPI.Infrastructure.Data;

/// <summary>
/// Startup guard: the API never migrates the database itself, so it reports (and optionally refuses to start on)
/// a database that is behind the EF migrations shipped with this build.
/// </summary>
public static class DatabaseSchemaCheck
{
    /// <returns>The pending migration ids; empty for non-relational providers (e.g. EF InMemory), which are skipped.</returns>
    public static async Task<IReadOnlyList<string>> EnsureUpToDateAsync(
        BookingDbContext context, ILogger logger, bool failOnPending)
    {
        if (!context.Database.IsRelational())
        {
            return Array.Empty<string>();
        }

        List<string> pending;
        try
        {
            pending = (await context.Database.GetPendingMigrationsAsync()).ToList();
        }
        catch (Exception ex) when (!failOnPending)
        {
            // With the check off (the Development default) an unreachable database must not stop startup.
            logger.LogWarning(ex, "Could not check the database schema for pending migrations.");
            return Array.Empty<string>();
        }

        if (pending.Count == 0)
        {
            return pending;
        }

        var message = "The database schema is behind this API version. Pending migrations: {Pending}. " +
                      "Apply them before serving traffic (see backend/README.md, \"Database migrations runbook\").";
        if (failOnPending)
        {
            logger.LogCritical(message, string.Join(", ", pending));
            throw new InvalidOperationException(
                $"Pending database migrations: {string.Join(", ", pending)}. See backend/README.md, \"Database migrations runbook\", " +
                "or set Database:FailOnPendingMigrations=false to start anyway.");
        }

        logger.LogWarning(message, string.Join(", ", pending));
        return pending;
    }
}
