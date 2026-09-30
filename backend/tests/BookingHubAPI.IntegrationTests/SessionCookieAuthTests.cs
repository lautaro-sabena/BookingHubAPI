using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BookingHubAPI.API.Authentication;
using BookingHubAPI.Application.DTOs;
using BookingHubAPI.IntegrationTests.Support;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace BookingHubAPI.IntegrationTests;

/// <summary>
/// The session JWT lives in an httpOnly cookie (browsers) while Authorization: Bearer keeps working (API clients).
/// Covers the cookie attributes, /me, logout and the CSRF header rule for cookie-authenticated requests.
/// </summary>
public class SessionCookieAuthTests : IClassFixture<BookingApiFactory>, IClassFixture<SessionCookieAuthTests.SecureCookieApiFactory>
{
    private readonly BookingApiFactory _factory;
    private readonly SecureCookieApiFactory _secureFactory;

    public SessionCookieAuthTests(BookingApiFactory factory, SecureCookieApiFactory secureFactory)
    {
        _factory = factory;
        _secureFactory = secureFactory;
    }

    /// <summary>Production-like cookie settings: <c>Auth:Cookie:Secure</c> is on (also the default outside Development).</summary>
    public class SecureCookieApiFactory : BookingApiFactory
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration((_, config) =>
                config.AddInMemoryCollection(new Dictionary<string, string?> { ["Auth:Cookie:Secure"] = "true" }));
            base.ConfigureWebHost(builder);
        }
    }

    private static string UniqueEmail() => $"cookie-{Guid.NewGuid():N}@test.com";

    private static HttpClient WithoutCsrfHeader(HttpClient client)
    {
        client.DefaultRequestHeaders.Remove(CsrfProtection.HeaderName);
        return client;
    }

    private static HttpRequestMessage ServicePost(string name = "Cookie service") =>
        new(HttpMethod.Post, "/api/services") { Content = JsonContent.Create(new ServiceRequest(name, "d", 60, 10m)) };

    // ---------- cookie attributes ----------

    [Theory]
    [InlineData("register")]
    [InlineData("login")]
    public async Task RegisterAndLogin_ShouldSetAnHttpOnlyLaxSecureCookieExpiringWithTheToken_AndNoTokenInTheBody(string endpoint)
    {
        var client = _secureFactory.CreateClient();
        var email = UniqueEmail();
        var register = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(email, "Password123!", "Owner"));

        var response = endpoint == "register"
            ? register
            : await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, "Password123!"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var cookie = response.SessionCookie();
        cookie.Should().NotBeNull();
        cookie!.Value.Should().NotBeEmpty();
        cookie.HasAttribute("httponly").Should().BeTrue();
        cookie.HasAttribute("secure").Should().BeTrue();
        cookie.AttributeValue("samesite").Should().Be("lax");
        cookie.AttributeValue("path").Should().Be("/");
        cookie.AttributeValue("domain").Should().BeNull("a host-only cookie is not shared with other subdomains");

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(cookie.Value);
        DateTimeOffset.Parse(cookie.AttributeValue("expires")!).UtcDateTime
            .Should().BeCloseTo(jwt.ValidTo, TimeSpan.FromSeconds(2));

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.EnumerateObject().Select(p => p.Name).Should().NotContain(n => n.Equals("token", StringComparison.OrdinalIgnoreCase));
        (await response.Content.ReadAsStringAsync()).Should().NotContain(cookie.Value);
    }

    [Fact]
    public async Task Login_InDevelopmentConfiguration_ShouldNotMarkTheCookieSecure()
    {
        var owner = await TestApi.RegisterOwnerAsync(_factory);

        var response = await _factory.CreateClient().PostAsJsonAsync("/api/auth/login", new LoginRequest(owner.Email, TestApi.Password));

        var cookie = response.SessionCookie();
        cookie.Should().NotBeNull();
        cookie!.HasAttribute("secure").Should().BeFalse("plain-http local development must be able to store the cookie");
        cookie.HasAttribute("httponly").Should().BeTrue();
    }

    [Fact]
    public async Task FailedLogin_ShouldNotSetTheCookie()
    {
        var response = await _factory.CreateClient().PostAsJsonAsync("/api/auth/login", new LoginRequest(UniqueEmail(), "Password123!"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.SessionCookie().Should().BeNull();
    }

    // ---------- GET /api/auth/me ----------

    [Fact]
    public async Task Me_WithTheSessionCookie_ShouldReturnTheUser()
    {
        var owner = await TestApi.RegisterOwnerAsync(_factory);

        var response = await _factory.CreateClient().SendAsync(new HttpRequestMessage(HttpMethod.Get, "/api/auth/me").WithSessionCookie(owner.Token));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.CacheControl!.NoStore.Should().BeTrue();
        var me = (await response.Content.ReadFromJsonAsync<UserDto>())!;
        me.Id.Should().Be(owner.UserId);
        me.Email.Should().Be(owner.Email);
        me.Role.Should().Be("Owner");
        me.CompanyId.Should().NotBeNull("owners get a company on registration");
    }

    [Fact]
    public async Task Me_ForACustomer_ShouldHaveNoCompany()
    {
        var customer = await TestApi.RegisterCustomerAsync(_factory);

        var me = (await (await _factory.CreateClient().SendAsync(new HttpRequestMessage(HttpMethod.Get, "/api/auth/me").WithSessionCookie(customer.Token)))
            .Content.ReadFromJsonAsync<UserDto>())!;

        me.Role.Should().Be("Customer");
        me.CompanyId.Should().BeNull();
    }

    [Fact]
    public async Task Me_WithoutACookieOrToken_ShouldBeAnUnauthorizedProblem()
    {
        var response = await _factory.CreateClient().GetAsync("/api/auth/me");

        await response.ShouldBeProblemAsync(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Me_WithAnInvalidCookie_ShouldBeUnauthorized()
    {
        var response = await _factory.CreateClient().SendAsync(new HttpRequestMessage(HttpMethod.Get, "/api/auth/me").WithSessionCookie("not-a-jwt"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Me_WithABearerToken_ShouldStillWork()
    {
        var customer = await TestApi.RegisterCustomerAsync(_factory);

        var response = await customer.Client.GetAsync("/api/auth/me");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadFromJsonAsync<UserDto>())!.Id.Should().Be(customer.UserId);
    }

    [Fact]
    public async Task AuthorizationHeader_ShouldWinOverTheCookie()
    {
        var bearerUser = await TestApi.RegisterCustomerAsync(_factory);
        var cookieUser = await TestApi.RegisterCustomerAsync(_factory);
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me").WithSessionCookie(cookieUser.Token);
        request.Headers.Authorization = new("Bearer", bearerUser.Token);

        var response = await _factory.CreateClient().SendAsync(request);

        (await response.Content.ReadFromJsonAsync<UserDto>())!.Id.Should().Be(bearerUser.UserId);
    }

    // ---------- POST /api/auth/logout ----------

    [Fact]
    public async Task Logout_ShouldExpireTheCookieWithTheSameAttributes()
    {
        var owner = await TestApi.RegisterOwnerAsync(_secureFactory);
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/logout").WithSessionCookie(owner.Token);

        var response = await _secureFactory.CreateClient().SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var cookie = response.SessionCookie();
        cookie.Should().NotBeNull();
        cookie!.IsExpired.Should().BeTrue();
        cookie.HasAttribute("httponly").Should().BeTrue();
        cookie.HasAttribute("secure").Should().BeTrue();
        cookie.AttributeValue("path").Should().Be("/");
        cookie.AttributeValue("samesite").Should().Be("lax");
    }

    [Fact]
    public async Task Logout_WithoutASession_ShouldStillSucceed()
    {
        var response = await _factory.CreateClient().PostAsync("/api/auth/logout", content: null);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Logout_WithoutTheCsrfHeader_ShouldBeForbidden()
    {
        var client = WithoutCsrfHeader(_factory.CreateClient());

        var response = await client.PostAsync("/api/auth/logout", content: null);

        await response.ShouldBeProblemAsync(HttpStatusCode.Forbidden);
        response.SessionCookie().Should().BeNull();
    }

    // ---------- CSRF header ----------

    [Fact]
    public async Task CookieAuthenticatedPost_WithoutTheCsrfHeader_ShouldBeForbiddenAndNotExecuted()
    {
        var owner = await TestApi.RegisterOwnerAsync(_factory);
        var client = WithoutCsrfHeader(_factory.CreateClient());

        var response = await client.SendAsync(ServicePost("Forged service").WithSessionCookie(owner.Token));

        var problem = await response.ShouldBeProblemAsync(HttpStatusCode.Forbidden);
        problem.Detail.Should().Contain(CsrfProtection.HeaderName);
        var own = await owner.Client.GetFromJsonAsync<JsonElement>("/api/services");
        own.ToString().Should().NotContain("Forged service");
    }

    [Fact]
    public async Task CookieAuthenticatedPost_WithAWrongCsrfHeaderValue_ShouldBeForbidden()
    {
        var owner = await TestApi.RegisterOwnerAsync(_factory);
        var client = WithoutCsrfHeader(_factory.CreateClient());
        var request = ServicePost().WithSessionCookie(owner.Token);
        request.Headers.Add(CsrfProtection.HeaderName, "XMLHttpRequest");

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Theory]
    [InlineData("POST")]
    [InlineData("PUT")]
    [InlineData("PATCH")]
    [InlineData("DELETE")]
    public async Task CookieAuthenticatedStateChange_WithoutTheCsrfHeader_ShouldBeForbidden(string method)
    {
        var owner = await TestApi.RegisterOwnerAsync(_factory);
        var client = WithoutCsrfHeader(_factory.CreateClient());
        var request = new HttpRequestMessage(new HttpMethod(method), $"/api/services/{Guid.NewGuid()}").WithSessionCookie(owner.Token);

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CookieAuthenticatedPost_WithTheCsrfHeader_ShouldSucceed()
    {
        var owner = await TestApi.RegisterOwnerAsync(_factory);

        var response = await _factory.CreateClient().SendAsync(ServicePost().WithSessionCookie(owner.Token));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task CookieAuthenticatedGet_WithoutTheCsrfHeader_ShouldSucceed()
    {
        var owner = await TestApi.RegisterOwnerAsync(_factory);
        var client = WithoutCsrfHeader(_factory.CreateClient());

        var response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Get, "/api/services").WithSessionCookie(owner.Token));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task BearerRequests_ShouldNotNeedTheCsrfHeader()
    {
        var owner = await TestApi.RegisterOwnerAsync(_factory);
        var client = WithoutCsrfHeader(_factory.CreateClient());
        var request = ServicePost();
        request.Headers.Authorization = new("Bearer", owner.Token);

        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task AnonymousPostToAProtectedEndpoint_WithoutCookieOrHeader_ShouldStayUnauthorized()
    {
        var client = WithoutCsrfHeader(_factory.CreateClient());

        var response = await client.SendAsync(ServicePost());

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData("/api/auth/login")]
    [InlineData("/api/auth/register")]
    public async Task LoginAndRegister_WithoutTheCsrfHeader_ShouldBeForbiddenAndSetNoCookie(string path)
    {
        var owner = await TestApi.RegisterOwnerAsync(_factory);
        var client = WithoutCsrfHeader(_factory.CreateClient());
        object body = path.EndsWith("login")
            ? new LoginRequest(owner.Email, TestApi.Password)
            : new RegisterRequest(UniqueEmail(), "Password123!", "Customer");

        var response = await client.PostAsJsonAsync(path, body);

        await response.ShouldBeProblemAsync(HttpStatusCode.Forbidden);
        response.SessionCookie().Should().BeNull();
    }
}
