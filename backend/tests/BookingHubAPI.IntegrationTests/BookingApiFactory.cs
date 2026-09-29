using AspNetCoreRateLimit;
using BookingHubAPI.Infrastructure.Data;
using BookingHubAPI.IntegrationTests.Support;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
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
    /// Program.cs reads Jwt:SecretKey and ConnectionStrings:DefaultConnection eagerly, right
    /// after WebApplication.CreateBuilder(args) and before Build() - so WebApplicationFactory's
    /// ConfigureAppConfiguration/ConfigureWebHost hooks (which only apply at Build() time) are
    /// too late to reach them. Environment variables, in contrast, are loaded by CreateBuilder
    /// itself, so setting them here - in a static constructor that runs before any test triggers
    /// host creation - reaches those eager reads. These are test-only values, never real secrets:
    /// appsettings.Development.json intentionally has neither key.
    /// </summary>
    static BookingApiFactory()
    {
        Environment.SetEnvironmentVariable("Jwt__SecretKey", "IntegrationTestSecretKey_1234567890123456");
        Environment.SetEnvironmentVariable(
            "ConnectionStrings__DefaultConnection",
            "Host=localhost;Port=5432;Database=bookinghub_test;Username=test;Password=test");
    }

    /// <summary>
    /// Business-flow tests (e.g. AuthControllerTests) call the same auth endpoints many
    /// times against one shared factory instance, which would otherwise trip the real
    /// production login/register rate-limit rules configured in appsettings.json. True (the
    /// default) replaces every configured rule with a single, effectively unlimited
    /// wildcard rule so functional tests aren't coupled to the auth throttle. Tests that
    /// exercise the real limiter override this to false to verify the production rules and
    /// their enforcement.
    /// </summary>
    protected virtual bool RelaxRateLimiting => true;

    /// <summary>The clock the API sees; real time unless a test pins it.</summary>
    public SettableTimeProvider Clock { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // EF Core 9 stores provider configuration in IDbContextOptionsConfiguration<T>;
            // both registrations must go, or Npgsql and InMemory end up registered together.
            services.RemoveAll<DbContextOptions<BookingDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<BookingDbContext>>();

            services.AddDbContext<BookingDbContext>(options =>
                // InMemory has no transactions: IUnitOfWork still runs its work, the transaction is a no-op.
                options.UseInMemoryDatabase(_databaseName)
                    .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning)));

            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(Clock);

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
