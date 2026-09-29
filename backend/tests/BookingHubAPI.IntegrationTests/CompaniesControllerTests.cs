using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BookingHubAPI.Application.DTOs;
using BookingHubAPI.IntegrationTests.Support;
using FluentAssertions;
using Xunit;

namespace BookingHubAPI.IntegrationTests;

/// <summary>
/// Characterization tests for /api/companies. They pin the observable HTTP behavior (status codes
/// and body shapes) so the controller can be refactored without changing it. Each test registers
/// its own owners/customers, so tests never interfere with each other.
/// </summary>
public class CompaniesControllerTests : IClassFixture<BookingApiFactory>
{
    private readonly BookingApiFactory _factory;

    public CompaniesControllerTests(BookingApiFactory factory)
    {
        _factory = factory;
    }

    private static async Task<CompanyResponse> GetMineAsync(TestUser owner)
    {
        var response = await owner.Client.GetAsync("/api/companies/me");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<CompanyResponse>())!;
    }

    // ---------- authentication ----------

    [Theory]
    [InlineData("GET", "/api/companies/me")]
    [InlineData("POST", "/api/companies")]
    [InlineData("PUT", "/api/companies/me")]
    public async Task AnyEndpoint_WithoutToken_ShouldReturnUnauthorized(string method, string url)
    {
        var client = _factory.CreateClient();

        var response = await client.SendAsync(new HttpRequestMessage(new HttpMethod(method), url));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ---------- GET /api/companies/me ----------

    [Fact]
    public async Task GetMyCompany_AsOwner_ShouldReturnTheCompanyCreatedAtRegistration()
    {
        var owner = await TestApi.RegisterOwnerAsync(_factory);

        var response = await owner.Client.GetAsync("/api/companies/me");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.EnumerateObject().Select(p => p.Name)
            .Should().BeEquivalentTo("id", "name", "description", "timeZone", "isActive", "ownerId");
        body.GetProperty("name").GetString().Should().Be($"{owner.Email}'s Company");
        body.GetProperty("description").ValueKind.Should().Be(JsonValueKind.Null);
        body.GetProperty("timeZone").GetString().Should().Be("UTC");
        body.GetProperty("isActive").GetBoolean().Should().BeTrue();
        body.GetProperty("ownerId").GetGuid().Should().Be(owner.UserId);
        body.GetProperty("id").GetGuid().Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetMyCompany_AsCustomer_ShouldReturnForbidden()
    {
        var customer = await TestApi.RegisterCustomerAsync(_factory);

        var response = await customer.Client.GetAsync("/api/companies/me");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetMyCompany_AsOwnerWithoutAnyCompany_ShouldReturnNotFound()
    {
        var owner = await TestApi.RegisterOwnerAsync(_factory);
        await TestApi.RemoveCompanyAsync(_factory, owner.UserId);

        var response = await owner.Client.GetAsync("/api/companies/me");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetMyCompany_TwoOwners_EachSeeOnlyTheirOwnCompany()
    {
        var ownerA = await TestApi.RegisterOwnerAsync(_factory);
        var ownerB = await TestApi.RegisterOwnerAsync(_factory);

        var companyA = await GetMineAsync(ownerA);
        var companyB = await GetMineAsync(ownerB);

        companyA.OwnerId.Should().Be(ownerA.UserId);
        companyB.OwnerId.Should().Be(ownerB.UserId);
        companyA.Id.Should().NotBe(companyB.Id);
    }

    // ---------- POST /api/companies ----------

    [Fact]
    public async Task CreateCompany_AsCustomer_ShouldReturnForbidden()
    {
        var customer = await TestApi.RegisterCustomerAsync(_factory);

        var response = await customer.Client.PostAsJsonAsync("/api/companies", new CompanyRequest("Acme", null, "UTC"));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateCompany_AsOwnerWhoAlreadyHasOne_ShouldReturnBadRequestAndKeepTheCompany()
    {
        var owner = await TestApi.RegisterOwnerAsync(_factory);
        var before = await GetMineAsync(owner);

        var response = await owner.Client.PostAsJsonAsync("/api/companies", new CompanyRequest("Second", null, "UTC"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("error").GetString().Should().Be("You already have a company");
        (await GetMineAsync(owner)).Should().Be(before);
    }

    [Fact]
    public async Task CreateCompany_AsOwnerWithoutCompany_ShouldReturnCreatedAndLinkTheOwner()
    {
        var owner = await TestApi.RegisterOwnerAsync(_factory);
        await TestApi.RemoveCompanyAsync(_factory, owner.UserId);

        var response = await owner.Client.PostAsJsonAsync(
            "/api/companies", new CompanyRequest("Acme", "Best in town", "Europe/Madrid"));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location!.AbsolutePath.Should().EndWith("/api/Companies/me");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.EnumerateObject().Select(p => p.Name)
            .Should().BeEquivalentTo("id", "name", "description", "timeZone", "isActive", "ownerId");
        body.GetProperty("name").GetString().Should().Be("Acme");
        body.GetProperty("description").GetString().Should().Be("Best in town");
        body.GetProperty("timeZone").GetString().Should().Be("Europe/Madrid");
        body.GetProperty("isActive").GetBoolean().Should().BeTrue();
        body.GetProperty("ownerId").GetGuid().Should().Be(owner.UserId);

        (await GetMineAsync(owner)).Id.Should().Be(body.GetProperty("id").GetGuid());
        // The owner is linked to the new company, so company-scoped endpoints now work (the token
        // predates the company and has no companyId claim: authorization reads the user from the database).
        var service = await owner.Client.PostAsJsonAsync("/api/services", new ServiceRequest("Cut", null, 30, 5m));
        service.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task CreateCompany_WithoutDescription_ShouldStoreNullDescription()
    {
        var owner = await TestApi.RegisterOwnerAsync(_factory);
        await TestApi.RemoveCompanyAsync(_factory, owner.UserId);

        var response = await owner.Client.PostAsJsonAsync("/api/companies", new CompanyRequest("Acme", null, "UTC"));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        (await response.Content.ReadFromJsonAsync<CompanyResponse>())!.Description.Should().BeNull();
    }

    [Theory]
    [InlineData("", "UTC")]
    [InlineData("Acme", "")]
    public async Task CreateCompany_WithMissingRequiredField_ShouldReturnBadRequest(string name, string timeZone)
    {
        var owner = await TestApi.RegisterOwnerAsync(_factory);
        await TestApi.RemoveCompanyAsync(_factory, owner.UserId);

        var response = await owner.Client.PostAsJsonAsync("/api/companies", new CompanyRequest(name, null, timeZone));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateCompany_WithTooLongName_ShouldReturnBadRequest()
    {
        var owner = await TestApi.RegisterOwnerAsync(_factory);
        await TestApi.RemoveCompanyAsync(_factory, owner.UserId);

        var response = await owner.Client.PostAsJsonAsync("/api/companies", new CompanyRequest(new string('a', 101), null, "UTC"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateCompany_WithUnknownTimeZone_ShouldReturnBadRequestAndNotCreateTheCompany()
    {
        var owner = await TestApi.RegisterOwnerAsync(_factory);
        await TestApi.RemoveCompanyAsync(_factory, owner.UserId);

        var response = await owner.Client.PostAsJsonAsync("/api/companies", new CompanyRequest("Acme", null, "Not/AZone"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync()).Should().Contain("Invalid time zone");
        (await owner.Client.GetAsync("/api/companies/me")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Theory]
    [InlineData("UTC")]
    [InlineData("America/Argentina/Buenos_Aires")]
    public async Task CreateCompany_WithKnownIanaTimeZone_ShouldBeAccepted(string timeZone)
    {
        var owner = await TestApi.RegisterOwnerAsync(_factory);
        await TestApi.RemoveCompanyAsync(_factory, owner.UserId);

        var response = await owner.Client.PostAsJsonAsync("/api/companies", new CompanyRequest("Acme", null, timeZone));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        (await GetMineAsync(owner)).TimeZone.Should().Be(timeZone);
    }

    [Fact]
    public async Task UpdateMyCompany_WithUnknownTimeZone_ShouldReturnBadRequestAndKeepTheCurrentOne()
    {
        var owner = await TestApi.RegisterOwnerAsync(_factory);

        var response = await owner.Client.PutAsJsonAsync("/api/companies/me", new CompanyUpdateRequest("Renamed", null, "Not/AZone"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var company = await GetMineAsync(owner);
        company.TimeZone.Should().Be("UTC");
        company.Name.Should().NotBe("Renamed");
    }

    // ---------- PUT /api/companies/me ----------

    [Fact]
    public async Task UpdateMyCompany_WithAllFields_ShouldReturnAndPersistTheUpdate()
    {
        var owner = await TestApi.RegisterOwnerAsync(_factory);
        var before = await GetMineAsync(owner);

        var response = await owner.Client.PutAsJsonAsync(
            "/api/companies/me", new CompanyUpdateRequest("Renamed", "New description", "America/Lima"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = (await response.Content.ReadFromJsonAsync<CompanyResponse>())!;
        updated.Should().Be(before with { Name = "Renamed", Description = "New description", TimeZone = "America/Lima" });
        (await GetMineAsync(owner)).Should().Be(updated);
    }

    [Fact]
    public async Task UpdateMyCompany_WithOnlyName_ShouldLeaveOtherFieldsUntouched()
    {
        var owner = await TestApi.RegisterOwnerAsync(_factory);
        await owner.Client.PutAsJsonAsync("/api/companies/me", new CompanyUpdateRequest(null, "Keep me", "America/Lima"));

        var response = await owner.Client.PutAsJsonAsync("/api/companies/me", new CompanyUpdateRequest("Only name", null, null));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = (await response.Content.ReadFromJsonAsync<CompanyResponse>())!;
        updated.Name.Should().Be("Only name");
        updated.Description.Should().Be("Keep me");
        updated.TimeZone.Should().Be("America/Lima");
    }

    [Fact]
    public async Task UpdateMyCompany_WithEmptyNameAndTimeZone_ShouldIgnoreThemButEmptyDescriptionClearsIt()
    {
        var owner = await TestApi.RegisterOwnerAsync(_factory);
        var before = await GetMineAsync(owner);
        await owner.Client.PutAsJsonAsync("/api/companies/me", new CompanyUpdateRequest(null, "Has description", null));

        var response = await owner.Client.PutAsJsonAsync("/api/companies/me", new CompanyUpdateRequest("", "", ""));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = (await response.Content.ReadFromJsonAsync<CompanyResponse>())!;
        updated.Name.Should().Be(before.Name);
        updated.TimeZone.Should().Be(before.TimeZone);
        updated.Description.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateMyCompany_WithEmptyBody_ShouldChangeNothing()
    {
        var owner = await TestApi.RegisterOwnerAsync(_factory);
        var before = await GetMineAsync(owner);

        var response = await owner.Client.PutAsJsonAsync("/api/companies/me", new CompanyUpdateRequest(null, null, null));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadFromJsonAsync<CompanyResponse>()).Should().Be(before);
    }

    [Fact]
    public async Task UpdateMyCompany_WithTooLongName_ShouldReturnBadRequest()
    {
        var owner = await TestApi.RegisterOwnerAsync(_factory);

        var response = await owner.Client.PutAsJsonAsync(
            "/api/companies/me", new CompanyUpdateRequest(new string('a', 101), null, null));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateMyCompany_AsCustomer_ShouldReturnNotFound()
    {
        // No role check: a customer simply owns no company.
        var customer = await TestApi.RegisterCustomerAsync(_factory);

        var response = await customer.Client.PutAsJsonAsync("/api/companies/me", new CompanyUpdateRequest("Hacked", null, null));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateMyCompany_AsOwnerWithoutAnyCompany_ShouldReturnNotFound()
    {
        var owner = await TestApi.RegisterOwnerAsync(_factory);
        await TestApi.RemoveCompanyAsync(_factory, owner.UserId);

        var response = await owner.Client.PutAsJsonAsync("/api/companies/me", new CompanyUpdateRequest("Nope", null, null));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateMyCompany_ShouldNeverTouchAnotherOwnersCompany()
    {
        var ownerA = await TestApi.RegisterOwnerAsync(_factory);
        var ownerB = await TestApi.RegisterOwnerAsync(_factory);
        var companyBBefore = await GetMineAsync(ownerB);

        var response = await ownerA.Client.PutAsJsonAsync(
            "/api/companies/me", new CompanyUpdateRequest("A renamed", "A description", "America/Lima"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadFromJsonAsync<CompanyResponse>())!.OwnerId.Should().Be(ownerA.UserId);
        (await GetMineAsync(ownerB)).Should().Be(companyBBefore);
    }

    [Fact]
    public async Task UpdateMyCompany_ShouldIgnoreIdAndOwnerIdSentInTheBody()
    {
        var ownerA = await TestApi.RegisterOwnerAsync(_factory);
        var ownerB = await TestApi.RegisterOwnerAsync(_factory);
        var companyA = await GetMineAsync(ownerA);
        var companyB = await GetMineAsync(ownerB);

        var response = await ownerA.Client.PutAsJsonAsync("/api/companies/me", new
        {
            id = companyB.Id,
            ownerId = ownerB.UserId,
            name = "Still mine"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await GetMineAsync(ownerA)).Should().Be(companyA with { Name = "Still mine" });
        (await GetMineAsync(ownerB)).Should().Be(companyB);
    }
}
