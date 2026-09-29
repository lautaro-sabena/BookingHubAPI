using BookingHubAPI.Infrastructure.Data;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace BookingHubAPI.UnitTests.Infrastructure;

public class ModelSnapshotTests
{
    /// <summary>
    /// Fails when the EF model changed without a migration. Fix it with
    /// <c>dotnet ef migrations add &lt;Name&gt; --project src/BookingHubAPI.Infrastructure</c> (see backend/README.md).
    /// Runs offline: the model is compared with the snapshot and no connection is opened.
    /// </summary>
    [Fact]
    public void Model_ShouldHaveNoChangesPendingAMigration()
    {
        // The factory sets the legacy timestamp switch and uses the same placeholder connection as design time.
        using var context = new BookingDbContextFactory().CreateDbContext([]);

        context.Database.HasPendingModelChanges().Should().BeFalse(
            "every model change needs a migration; run `dotnet ef migrations add <Name>`");
    }
}
