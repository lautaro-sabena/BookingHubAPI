using AspNetCoreRateLimit;
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

    /// <summary>
    /// Business-flow tests (e.g. AuthControllerTests) call the same auth endpoints many
    /// times against one shared factory instance, which would otherwise trip the real
    /// production login/register rate-limit rules loaded from rate-limit.json. True (the
    /// default) relaxes those rules to a large limit so functional tests aren't coupled to
    /// the auth throttle. RateLimitingTests overrides this to false to verify the real
    /// rules and their enforcement.
    /// </summary>
    protected virtual bool RelaxRateLimiting => true;

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

            if (RelaxRateLimiting)
            {
                services.PostConfigure<IpRateLimitOptions>(options =>
                {
                    options.GeneralRules = new List<RateLimitRule>
                    {
                        new() { Endpoint = "*", Period = "1s", Limit = 100_000 }
                    };
                });
            }
        });
    }
}
