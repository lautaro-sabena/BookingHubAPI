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

    /// <summary>Creates an active service through POST /api/services as the given owner.</summary>
    public static async Task<ServiceResponse> CreateServiceAsync(TestUser owner, int durationMinutes = 60)
    {
        var response = await owner.Client.PostAsJsonAsync(
            "/api/services",
            new ServiceRequest($"Service {Guid.NewGuid():N}", "Test service", durationMinutes, 10m));
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<ServiceResponse>())!;
    }

    /// <summary>Marks a company inactive; no endpoint exposes this state.</summary>
    public static async Task DeactivateCompanyAsync(BookingApiFactory factory, Guid companyId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BookingDbContext>();
        var company = await db.Companies.FindAsync(companyId);
        company!.IsActive = false;
        await db.SaveChangesAsync();
    }

    /// <summary>A start time comfortably in the future, on a whole hour, offset by whole days.</summary>
    public static DateTime FutureSlot(int daysAhead = 30, int hour = 10) =>
        DateTime.SpecifyKind(DateTime.UtcNow.Date.AddDays(daysAhead).AddHours(hour), DateTimeKind.Utc);
}
