using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using BookingHubAPI.Application.DTOs;
using BookingHubAPI.Infrastructure.Data;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace BookingHubAPI.IntegrationTests.Support;

/// <summary>An authenticated caller registered through the real auth endpoints.</summary>
public sealed record TestUser(HttpClient Client, Guid UserId, string Email, string Role, string Token);

/// <summary>
/// Shared setup helpers for integration tests. Every helper goes through the public HTTP API
/// (register/login/services) so tests exercise the same paths real clients use; only the few
/// states no endpoint can produce (e.g. an inactive company) are written straight to the database.
/// </summary>
public static class TestApi
{
    public const string Password = "Password123!";

    public static Task<TestUser> RegisterOwnerAsync(BookingApiFactory factory) =>
        RegisterAsync(factory, "Owner");

    public static Task<TestUser> RegisterCustomerAsync(BookingApiFactory factory) =>
        RegisterAsync(factory, "Customer");

    /// <summary>Registers a user with a unique e-mail, logs in, and returns a client carrying the login token.</summary>
    public static async Task<TestUser> RegisterAsync(BookingApiFactory factory, string role)
    {
        var email = $"{role.ToLowerInvariant()}-{Guid.NewGuid():N}@test.com";
        var anonymous = factory.CreateClient();

        var register = await anonymous.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, Password, role));
        register.EnsureSuccessStatusCode();

        var login = await anonymous.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, Password));
        login.EnsureSuccessStatusCode();
        var token = (await login.Content.ReadFromJsonAsync<TokenResponse>())!;

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);
        return new TestUser(client, token.UserId, token.Email, token.Role, token.Token);
    }

    /// <summary>
    /// Creates an active service through POST /api/services as the given owner. By default the owner's
    /// company is opened every day of the week from <see cref="OpeningHour"/> to <see cref="ClosingHour"/>,
    /// because bookings are only accepted inside working hours; pass <paramref name="openAllWeek"/> false
    /// to leave the schedule untouched.
    /// </summary>
    public static async Task<ServiceResponse> CreateServiceAsync(
        TestUser owner, int durationMinutes = 60, bool openAllWeek = true)
    {
        if (openAllWeek)
        {
            await SetWorkingHoursAsync(
                owner,
                Enum.GetValues<DayOfWeek>().Select(day => Hours(day, OpeningHour, ClosingHour)).ToArray());
        }

        var response = await owner.Client.PostAsJsonAsync(
            "/api/services",
            new ServiceRequest($"Service {Guid.NewGuid():N}", "Test service", durationMinutes, 10m));
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<ServiceResponse>())!;
    }

    /// <summary>Replaces the owner's weekly schedule through PUT /api/workinghours.</summary>
    public static async Task SetWorkingHoursAsync(TestUser owner, params WorkingHoursRequest[] days)
    {
        var response = await owner.Client.PutAsJsonAsync("/api/workinghours", days.ToList());
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    /// <summary>One schedule entry on whole hours.</summary>
    public static WorkingHoursRequest Hours(DayOfWeek day, int startHour, int endHour, bool isActive = true) =>
        new(day, new TimeSpan(startHour, 0, 0), new TimeSpan(endHour, 0, 0), isActive);

    /// <summary>Marks a company inactive; no endpoint exposes this state.</summary>
    public static async Task DeactivateCompanyAsync(BookingApiFactory factory, Guid companyId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BookingDbContext>();
        var company = await db.Companies.FindAsync(companyId);
        company!.IsActive = false;
        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Leaves an owner without a company; no endpoint produces this state, but a failed
    /// half-way owner registration can (user and company are written separately).
    /// </summary>
    public static async Task DetachFromCompanyAsync(BookingApiFactory factory, Guid userId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BookingDbContext>();
        var user = await db.Users.FindAsync(userId);
        user!.CompanyId = null;
        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Deletes an owner's company and clears the link, leaving an owner who has never created one;
    /// no endpoint produces this state (registration always creates the company).
    /// </summary>
    public static async Task RemoveCompanyAsync(BookingApiFactory factory, Guid ownerId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BookingDbContext>();
        var user = await db.Users.FindAsync(ownerId);
        user!.CompanyId = null;
        db.Companies.RemoveRange(db.Companies.Where(c => c.OwnerId == ownerId));
        await db.SaveChangesAsync();
    }

    /// <summary>Opening and closing hour of the schedule <see cref="CreateServiceAsync"/> gives every company.</summary>
    public const int OpeningHour = 9;

    public const int ClosingHour = 17;

    /// <summary>
    /// A start time comfortably in the future, on a whole hour, offset by whole days. The default hour
    /// lies inside the default 09:00-17:00 schedule (any weekday), so bookings made with it are accepted.
    /// </summary>
    public static DateTime FutureSlot(int daysAhead = 30, int hour = 10) =>
        DateTime.SpecifyKind(DateTime.UtcNow.Date.AddDays(daysAhead).AddHours(hour), DateTimeKind.Utc);
}
