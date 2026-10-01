using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BookingHubAPI.Application.DTOs;
using BookingHubAPI.IntegrationTests.Support;
using FluentAssertions;
using Xunit;

namespace BookingHubAPI.IntegrationTests;

/// <summary>
/// Characterization tests for /api/auth beyond the happy paths in <see cref="AuthControllerTests"/>:
/// error bodies, input validation, the token claims and the company created for owners. They pin
/// today's observable behavior so the controller can be refactored without changing it. Every test
/// registers unique e-mails, so tests never interfere with each other.
/// </summary>
public class AuthControllerCharacterizationTests : IClassFixture<BookingApiFactory>
{
    private readonly BookingApiFactory _factory;
    private readonly HttpClient _anonymous;

    public AuthControllerCharacterizationTests(BookingApiFactory factory)
    {
        _factory = factory;
        _anonymous = factory.CreateClient();
    }

    private static string UniqueEmail(string prefix = "user") => $"{prefix}-{Guid.NewGuid():N}@test.com";

    private static async Task<string> ErrorOf(HttpResponseMessage response)
    {
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        return json.GetProperty("error").GetString()!;
    }

    private static JwtSecurityToken Decode(string token) => new JwtSecurityTokenHandler().ReadJwtToken(token);

    private static string? Claim(JwtSecurityToken token, string type) =>
        token.Claims.FirstOrDefault(c => c.Type == type)?.Value;

    private static string? RoleClaim(JwtSecurityToken token) =>
        token.Claims.FirstOrDefault(c => c.Type == "role" || c.Type.EndsWith("/claims/role"))?.Value;

    // ---------- POST /api/auth/register ----------

    [Fact]
    public async Task Register_Customer_ShouldReturnTokenResponseWithAllFields()
    {
        var email = UniqueEmail("customer");

        var response = await _anonymous.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, "Password123!", "Customer"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.EnumerateObject().Select(p => p.Name).Should().BeEquivalentTo("token", "userId", "email", "role");
        body.GetProperty("email").GetString().Should().Be(email);
        body.GetProperty("role").GetString().Should().Be("Customer");
        body.GetProperty("userId").GetGuid().Should().NotBeEmpty();
        body.GetProperty("token").GetString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Register_Customer_TokenShouldCarryUserIdEmailAndRoleButNoCompanyId()
    {
        var email = UniqueEmail("customer");

        var response = await _anonymous.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, "Password123!", "Customer"));
        var registered = (await response.Content.ReadFromJsonAsync<TokenResponse>())!;

        var token = Decode(registered.Token);
        Claim(token, "sub").Should().Be(registered.UserId.ToString());
        Claim(token, "userId").Should().Be(registered.UserId.ToString());
        Claim(token, "email").Should().Be(email);
        RoleClaim(token).Should().Be("Customer");
        Claim(token, "companyId").Should().BeNull();
        Claim(token, "jti").Should().NotBeNullOrEmpty();
        token.ValidTo.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public async Task Register_Owner_ShouldCreateCompanyAndTokenShouldCarryRoleAndCompanyId()
    {
        var email = UniqueEmail("owner");

        var response = await _anonymous.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, "Password123!", "Owner"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var registered = (await response.Content.ReadFromJsonAsync<TokenResponse>())!;
        registered.Role.Should().Be("Owner");

        var token = Decode(registered.Token);
        RoleClaim(token).Should().Be("Owner");
        Claim(token, "userId").Should().Be(registered.UserId.ToString());
        var companyId = Claim(token, "companyId");
        companyId.Should().NotBeNullOrEmpty();

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", registered.Token);
        var company = await client.GetFromJsonAsync<CompanyResponse>("/api/companies/me");
        company!.Id.ToString().Should().Be(companyId);
        company.OwnerId.Should().Be(registered.UserId);
        company.Name.Should().Be($"{email}'s Company");
        company.TimeZone.Should().Be("UTC");
        company.IsActive.Should().BeTrue();
        company.Description.Should().BeNull();
    }

    [Fact]
    public async Task Register_Owner_LoginTokenShouldAlsoCarryCompanyId()
    {
        var owner = await TestApi.RegisterOwnerAsync(_factory);

        var token = Decode(owner.Token);

        Claim(token, "companyId").Should().NotBeNullOrEmpty();
        RoleClaim(token).Should().Be("Owner");
    }

    [Theory]
    [InlineData("owner", "Owner")]
    [InlineData("OWNER", "Owner")]
    [InlineData("customer", "Customer")]
    public async Task Register_RoleIsCaseInsensitiveAndNormalizedInResponse(string sentRole, string expectedRole)
    {
        var response = await _anonymous.PostAsJsonAsync("/api/auth/register", new RegisterRequest(UniqueEmail(), "Password123!", sentRole));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadFromJsonAsync<TokenResponse>())!.Role.Should().Be(expectedRole);
    }

    [Theory]
    [InlineData("Admin")]
    [InlineData("InvalidRole")]
    public async Task Register_WithUnknownRole_ShouldReturnBadRequestWithErrorMessage(string role)
    {
        var response = await _anonymous.PostAsJsonAsync("/api/auth/register", new RegisterRequest(UniqueEmail(), "Password123!", role));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await ErrorOf(response)).Should().Be("Invalid role. Must be 'Owner' or 'Customer'");
    }

    [Fact]
    public async Task Register_WithUndefinedNumericRole_ShouldSucceedWithThatRole()
    {
        // CURRENT BEHAVIOR (bug): Enum.TryParse accepts any number, so "5" passes the role check and
        // creates a user whose role is neither Owner nor Customer. It is harmless for authorization
        // (every [Authorize(Roles = ...)] rejects it) but the role validation is porous.
        var email = UniqueEmail();

        var response = await _anonymous.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, "Password123!", "5"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var registered = (await response.Content.ReadFromJsonAsync<TokenResponse>())!;
        registered.Role.Should().Be("5");

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new("Bearer", registered.Token);
        (await client.GetAsync("/api/favorites")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await client.GetAsync("/api/services")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ShouldReturnBadRequestWithErrorMessage()
    {
        var email = UniqueEmail();
        await _anonymous.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, "Password123!", "Customer"));

        var response = await _anonymous.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, "Other123!", "Owner"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await ErrorOf(response)).Should().Be("Email already registered");
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ShouldNotChangeTheExistingPassword()
    {
        var email = UniqueEmail();
        await _anonymous.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, "Password123!", "Customer"));
        await _anonymous.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, "Hijacked123!", "Customer"));

        var original = await _anonymous.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, "Password123!"));
        var hijack = await _anonymous.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, "Hijacked123!"));

        original.StatusCode.Should().Be(HttpStatusCode.OK);
        hijack.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Register_WithInvalidRoleAndDuplicateEmail_ShouldReportTheRoleFirst()
    {
        var email = UniqueEmail();
        await _anonymous.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, "Password123!", "Customer"));

        var response = await _anonymous.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, "Password123!", "Nope"));

        (await ErrorOf(response)).Should().StartWith("Invalid role");
    }

    [Fact]
    public async Task Register_EmailsDifferingOnlyByCase_AreTreatedAsDifferentUsers()
    {
        // CURRENT BEHAVIOR (bug): e-mail uniqueness and lookup are case-sensitive, so
        // "Name@x.com" and "name@x.com" can both register and log in as separate accounts.
        var lower = UniqueEmail();
        var upper = lower.ToUpperInvariant();

        var first = await _anonymous.PostAsJsonAsync("/api/auth/register", new RegisterRequest(lower, "Password123!", "Customer"));
        var second = await _anonymous.PostAsJsonAsync("/api/auth/register", new RegisterRequest(upper, "Password123!", "Customer"));

        first.StatusCode.Should().Be(HttpStatusCode.OK);
        second.StatusCode.Should().Be(HttpStatusCode.OK);
        (await first.Content.ReadFromJsonAsync<TokenResponse>())!.UserId
            .Should().NotBe((await second.Content.ReadFromJsonAsync<TokenResponse>())!.UserId);
    }

    [Theory]
    [InlineData("not-an-email", "Password123!", "Customer")]
    [InlineData("", "Password123!", "Customer")]
    [InlineData("valid@test.com", "short", "Customer")]
    [InlineData("valid@test.com", "Password123!", "")]
    public async Task Register_WithInvalidInput_ShouldReturnBadRequest(string email, string password, string role)
    {
        var response = await _anonymous.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, password, role));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_WithTooLongPassword_ShouldReturnBadRequest()
    {
        var response = await _anonymous.PostAsJsonAsync(
            "/api/auth/register", new RegisterRequest(UniqueEmail(), new string('a', 101), "Customer"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_WithInvalidInput_ShouldNotCreateTheUser()
    {
        var email = UniqueEmail();
        await _anonymous.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, "short", "Customer"));

        var login = await _anonymous.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, "short"));

        login.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ---------- POST /api/auth/login ----------

    [Fact]
    public async Task Login_ShouldReturnTokenResponseMatchingTheRegisteredUser()
    {
        var email = UniqueEmail("owner");
        var registered = (await (await _anonymous.PostAsJsonAsync(
            "/api/auth/register", new RegisterRequest(email, "Password123!", "Owner"))).Content.ReadFromJsonAsync<TokenResponse>())!;

        var response = await _anonymous.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, "Password123!"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.EnumerateObject().Select(p => p.Name).Should().BeEquivalentTo("token", "userId", "email", "role");
        body.GetProperty("userId").GetGuid().Should().Be(registered.UserId);
        body.GetProperty("email").GetString().Should().Be(email);
        body.GetProperty("role").GetString().Should().Be("Owner");
    }

    [Fact]
    public async Task Login_WithWrongPasswordOrUnknownEmail_ShouldReturnTheSameResponse()
    {
        var email = UniqueEmail();
        await _anonymous.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, "Password123!", "Customer"));

        var wrongPassword = await _anonymous.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, "WrongPassword1!"));
        var unknownEmail = await _anonymous.PostAsJsonAsync("/api/auth/login", new LoginRequest(UniqueEmail(), "Password123!"));

        wrongPassword.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        unknownEmail.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await ErrorOf(wrongPassword)).Should().Be("Invalid email or password");
        (await unknownEmail.Content.ReadAsStringAsync()).Should().Be(await wrongPassword.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Login_EmailIsCaseSensitive()
    {
        // CURRENT BEHAVIOR (bug): the e-mail lookup is an exact match, so the upper-cased address of a
        // registered user is rejected as unknown.
        var email = UniqueEmail();
        await _anonymous.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, "Password123!", "Customer"));

        var response = await _anonymous.PostAsJsonAsync("/api/auth/login", new LoginRequest(email.ToUpperInvariant(), "Password123!"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData("not-an-email", "Password123!")]
    [InlineData("", "Password123!")]
    [InlineData("valid@test.com", "")]
    public async Task Login_WithInvalidInput_ShouldReturnBadRequest(string email, string password)
    {
        var response = await _anonymous.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, password));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_TokenShouldBeAcceptedByProtectedEndpoints()
    {
        var customer = await TestApi.RegisterCustomerAsync(_factory);

        var response = await customer.Client.GetAsync("/api/favorites");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Login_ResponseShouldNotExposeThePasswordHash()
    {
        var email = UniqueEmail();
        await _anonymous.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, "Password123!", "Customer"));

        var response = await _anonymous.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, "Password123!"));

        var raw = await response.Content.ReadAsStringAsync();
        raw.ToLowerInvariant().Should().NotContain("hash").And.NotContain("$2");
    }
}
