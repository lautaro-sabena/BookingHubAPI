using BookingHubAPI.Infrastructure.Data;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace BookingHubAPI.UnitTests.Infrastructure;

public class DatabaseSchemaCheckTests
{
    // Port 1 on loopback refuses the connection immediately; no real database is involved.
    private const string UnreachableConnection = "Host=127.0.0.1;Port=1;Database=none;Username=none;Password=none;Timeout=2";

    [Fact]
    public async Task UnreachableDatabase_WhenFailOnPendingIsOff_ShouldLogAndContinue()
    {
        await using var context = CreateUnreachableContext();

        var pending = await DatabaseSchemaCheck.EnsureUpToDateAsync(context, NullLogger.Instance, failOnPending: false);

        pending.Should().BeEmpty();
    }

    [Fact]
    public async Task UnreachableDatabase_WhenFailOnPendingIsOn_ShouldThrow()
    {
        await using var context = CreateUnreachableContext();

        var act = () => DatabaseSchemaCheck.EnsureUpToDateAsync(context, NullLogger.Instance, failOnPending: true);

        await act.Should().ThrowAsync<Exception>();
    }

    private static BookingDbContext CreateUnreachableContext()
    {
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
        var options = new DbContextOptionsBuilder<BookingDbContext>()
            .UseNpgsql(UnreachableConnection)
            .Options;
        return new BookingDbContext(options);
    }
}
