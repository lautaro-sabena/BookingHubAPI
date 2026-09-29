using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using BookingHubAPI.Application.DTOs;
using BookingHubAPI.IntegrationTests.Support;
using FluentAssertions;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace BookingHubAPI.IntegrationTests;

/// <summary>
/// Every error response, whichever layer produces it, is an RFC 7807 problem+json body with
/// status, title and traceId; <c>detail</c> carries the human-readable message when there is one.
/// </summary>
public class ErrorResponsesTests : IClassFixture<BookingApiFactory>
{
    private static readonly Guid UnknownId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    private readonly BookingApiFactory _factory;

    public ErrorResponsesTests(BookingApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task NotFoundResult_ShouldBeAProblemWithDetail()
    {
        var owner = await TestApi.RegisterOwnerAsync(_factory);

        var response = await owner.Client.GetAsync($"/api/services/{UnknownId}");

        var problem = await response.ShouldBeProblemAsync(HttpStatusCode.NotFound, "Service not found");
        problem.Title.Should().Be("Not Found");
    }

    [Fact]
    public async Task AvailabilityForUnknownService_ShouldBeANotFoundProblem()
    {
        var customer = await TestApi.RegisterCustomerAsync(_factory);

        var response = await customer.Client.GetAsync($"/api/availability/{UnknownId}?date=2030-01-01");

        await response.ShouldBeProblemAsync(HttpStatusCode.NotFound, "Service not found or inactive");
    }

    [Fact]
    public async Task ForbiddenResult_ShouldBeAProblemWithDetail()
    {
        var owner = await TestApi.RegisterOwnerAsync(_factory);
        var service = await TestApi.CreateServiceAsync(owner);
        var customer = await TestApi.RegisterCustomerAsync(_factory);
        var stranger = await TestApi.RegisterCustomerAsync(_factory);
        var booked = await customer.Client.PostAsJsonAsync(
            "/api/reservations", new ReservationRequest(service.Id, TestApi.FutureSlot(), null));
        var reservation = (await booked.Content.ReadFromJsonAsync<ReservationResponse>())!;

        var response = await stranger.Client.PutAsync($"/api/reservations/{reservation.Id}/cancel", null);

        var problem = await response.ShouldBeProblemAsync(HttpStatusCode.Forbidden);
        problem.Title.Should().Be("Forbidden");
        problem.Detail.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task RoleForbidden_ShouldBeAProblem()
    {
        var customer = await TestApi.RegisterCustomerAsync(_factory);

        var response = await customer.Client.GetAsync("/api/services");

        var problem = await response.ShouldBeProblemAsync(HttpStatusCode.Forbidden);
        problem.Title.Should().Be("Forbidden");
    }

    [Fact]
    public async Task MissingToken_ShouldBeAnUnauthorizedProblem()
    {
        var response = await _factory.CreateClient().GetAsync("/api/services/all");

        var problem = await response.ShouldBeProblemAsync(HttpStatusCode.Unauthorized);
        problem.Title.Should().Be("Unauthorized");
    }

    [Fact]
    public async Task UnknownRoute_ShouldBeANotFoundProblem()
    {
        var response = await _factory.CreateClient().GetAsync("/api/does-not-exist");

        await response.ShouldBeProblemAsync(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ConflictResult_ShouldBeAProblemWithDetail()
    {
        var owner = await TestApi.RegisterOwnerAsync(_factory);
        var service = await TestApi.CreateServiceAsync(owner);
        var customer = await TestApi.RegisterCustomerAsync(_factory);
        var body = new ReservationRequest(service.Id, TestApi.FutureSlot(), null);
        (await customer.Client.PostAsJsonAsync("/api/reservations", body)).EnsureSuccessStatusCode();

        var response = await customer.Client.PostAsJsonAsync("/api/reservations", body);

        var problem = await response.ShouldBeProblemAsync(HttpStatusCode.Conflict, "Time slot is not available");
        problem.Title.Should().Be("Conflict");
    }

    [Fact]
    public async Task ModelValidationFailure_ShouldBeAValidationProblem()
    {
        var owner = await TestApi.RegisterOwnerAsync(_factory);

        var response = await owner.Client.PostAsJsonAsync(
            "/api/services", new ServiceRequest("", "x", 30, 10m));

        var problem = await response.ReadProblemAsync();
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        problem.Extensions.Should().ContainKey("errors");
    }

    [Fact]
    public async Task ValidTokenWithoutUserIdClaim_ShouldBeAnUnauthorizedProblem()
    {
        var response = await GetFavoritesWithTokenAsync(Token());

        var problem = await response.ShouldBeProblemAsync(HttpStatusCode.Unauthorized);
        problem.Detail.Should().Be("The access token does not identify a valid user.");
    }

    [Fact]
    public async Task ValidTokenWithMalformedUserId_ShouldBeAnUnauthorizedProblem()
    {
        var response = await GetFavoritesWithTokenAsync(Token(new Claim(JwtRegisteredClaimNames.Sub, "not-a-guid")));

        await response.ShouldBeProblemAsync(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task HandledException_WhenClientOnlyAcceptsHtml_ShouldStillReturnTheErrorStatus()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Token());
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/html"));

        var response = await client.GetAsync("/api/favorites");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private async Task<HttpResponseMessage> GetFavoritesWithTokenAsync(string token)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await client.GetAsync("/api/favorites");
    }

    /// <summary>A correctly signed Customer token carrying only the given extra claims (no user id unless passed).</summary>
    private static string Token(params Claim[] extra)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("IntegrationTestSecretKey_1234567890123456"));
        var token = new JwtSecurityToken(
            issuer: "BookingHubAPI",
            audience: "BookingHubAPI",
            claims: extra.Append(new Claim(ClaimTypes.Role, "Customer")),
            expires: DateTime.UtcNow.AddMinutes(5),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
