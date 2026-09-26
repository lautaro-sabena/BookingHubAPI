using BookingHubAPI.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BookingHubAPI.IntegrationTests;

/// <summary>
/// Boots the API against an isolated in-memory database shared by every
/// DbContext created within this factory instance.
/// </summary>
public class BookingApiFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = "TestDb_" + Guid.NewGuid();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // EF Core 9 stores provider configuration in IDbContextOptionsConfiguration<T>;
            // both registrations must go, or Npgsql and InMemory end up registered together.
            services.RemoveAll<DbContextOptions<BookingDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<BookingDbContext>>();

            services.AddDbContext<BookingDbContext>(options =>
                options.UseInMemoryDatabase(_databaseName));
        });
    }
}
