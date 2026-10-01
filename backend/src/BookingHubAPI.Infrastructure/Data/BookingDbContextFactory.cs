using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace BookingHubAPI.Infrastructure.Data;

/// <summary>
/// Used only by the <c>dotnet ef</c> tooling. <c>migrations add</c> and <c>migrations script</c> never
/// open a connection, so the connection string below is a placeholder and no secret is needed.
/// <c>dotnet ef database update</c> does connect: pass <c>--connection</c> explicitly.
/// </summary>
public class BookingDbContextFactory : IDesignTimeDbContextFactory<BookingDbContext>
{
    public BookingDbContext CreateDbContext(string[] args)
    {
        // Must match Program.cs: the switch decides how DateTime maps (timestamp without time zone),
        // so the generated migrations and snapshot describe the schema the app really uses.
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

        var options = new DbContextOptionsBuilder<BookingDbContext>()
            .UseNpgsql("Host=localhost;Database=bookinghub_design_time;Username=design;Password=design")
            .Options;
        return new BookingDbContext(options);
    }
}
