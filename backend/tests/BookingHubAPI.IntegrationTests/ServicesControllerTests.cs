using System.Net;
using System.Net.Http.Json;
using BookingHubAPI.Application.DTOs;
using BookingHubAPI.IntegrationTests.Support;
using FluentAssertions;
using Xunit;

namespace BookingHubAPI.IntegrationTests;

/// <summary>
/// Characterization tests for /api/services. They pin the observable HTTP behavior (status codes
/// and body shapes) so the controller can be refactored without changing it. Each test registers
/// its own owners/customers, so tests never interfere with each other.
/// </summary>
public class ServicesControllerTests : IClassFixture<BookingApiFactory>
{
    private static readonly Guid UnknownId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    private readonly BookingApiFactory _factory;

    public ServicesControllerTests(BookingApiFactory factory)
    {
        _factory = factory;
    }

    // ---------- helpers ----------

    private static async Task<PagedResult<ServiceResponse>> ListAsync(TestUser user, string url)
    {
        var response = await user.Client.GetAsync(url);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<PagedResult<ServiceResponse>>())!;
    }

    private static Task<HttpResponseMessage> UpdateAsync(TestUser user, Guid id, ServiceUpdateRequest request) =>
        user.Client.PutAsJsonAsync($"/api/services/{id}", request);

    private static async Task<ServiceResponse> GetOkAsync(TestUser user, Guid id)
    {
        var response = await user.Client.GetAsync($"/api/services/{id}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<ServiceResponse>())!;
    }

    private static async Task<ServiceResponse> CreateNamedAsync(TestUser owner, string name)
    {
        var response = await owner.Client.PostAsJsonAsync("/api/services", new ServiceRequest(name, null, 30, 5m));
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<ServiceResponse>())!;
    }

    private static async Task DeactivateAsync(TestUser owner, ServiceResponse service)
    {
        var response = await owner.Client.DeleteAsync($"/api/services/{service.Id}");
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    // ---------- authentication ----------

    [Theory]
    [InlineData("GET", "/api/services")]
    [InlineData("GET", "/api/services/all")]
    [InlineData("GET", "/api/services/00000000-0000-0000-0000-000000000001")]
    [InlineData("POST", "/api/services")]
    [InlineData("PUT", "/api/services/00000000-0000-0000-0000-000000000001")]
    [InlineData("DELETE", "/api/services/00000000-0000-0000-0000-000000000001")]
    public async Task AnyEndpoint_WithoutToken_ShouldReturnUnauthorized(string method, string url)
    {
        var client = _factory.CreateClient();

        var response = await client.SendAsync(new HttpRequestMessage(new HttpMethod(method), url));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ---------- GET /api/services (owner's own catalog) ----------

    [Fact]
    public async Task GetServices_AsCustomer_ShouldReturnForbidden()
    {
        var customer = await TestApi.RegisterCustomerAsync(_factory);

        var response = await customer.Client.GetAsync("/api/services");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetServices_AsOwnerWithoutCompany_ShouldReturnForbidden()
    {
        var owner = await TestApi.RegisterOwnerAsync(_factory);
        await TestApi.DetachFromCompanyAsync(_factory, owner.UserId);

        var response = await owner.Client.GetAsync("/api/services");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetServices_WithoutServices_ShouldReturnEmptyPage()
    {
        var owner = await TestApi.RegisterOwnerAsync(_factory);

        var page = await ListAsync(owner, "/api/services");

        page.Items.Should().BeEmpty();
        page.TotalCount.Should().Be(0);
        page.TotalPages.Should().Be(0);
        page.Page.Should().Be(1);
        page.PageSize.Should().Be(10);
    }

    [Fact]
    public async Task GetServices_ShouldReturnOnlyOwnActiveServicesWithCompanyData()
    {
        var owner = await TestApi.RegisterOwnerAsync(_factory);
        var otherOwner = await TestApi.RegisterOwnerAsync(_factory);
        var mine = await TestApi.CreateServiceAsync(owner);
        var removed = await TestApi.CreateServiceAsync(owner);
        await DeactivateAsync(owner, removed);
        var theirs = await TestApi.CreateServiceAsync(otherOwner);

        var page = await ListAsync(owner, "/api/services");

        page.Items.Should().ContainSingle();
        var item = page.Items.Single();
        item.Should().BeEquivalentTo(mine);
        item.CompanyId.Should().NotBe(theirs.CompanyId);
        item.IsActive.Should().BeTrue();
        page.TotalCount.Should().Be(1);
        page.TotalPages.Should().Be(1);
    }

    [Fact]
    public async Task GetServices_ShouldPaginate()
    {
        var owner = await TestApi.RegisterOwnerAsync(_factory);
        for (var i = 0; i < 5; i++)
        {
            await TestApi.CreateServiceAsync(owner);
        }

        var second = await ListAsync(owner, "/api/services?page=2&pageSize=2");
        var last = await ListAsync(owner, "/api/services?page=3&pageSize=2");
        var beyond = await ListAsync(owner, "/api/services?page=4&pageSize=2");

        second.Items.Should().HaveCount(2);
        second.TotalCount.Should().Be(5);
        second.TotalPages.Should().Be(3);
        second.Page.Should().Be(2);
        second.PageSize.Should().Be(2);
        last.Items.Should().HaveCount(1);
        beyond.Items.Should().BeEmpty();
        beyond.TotalCount.Should().Be(5);
    }

    [Fact]
    public async Task GetServices_WithSearch_ShouldFilterByNameSubstring()
    {
        var owner = await TestApi.RegisterOwnerAsync(_factory);
        var token = Guid.NewGuid().ToString("N");
        var match = await CreateNamedAsync(owner, $"Haircut {token}");
        await CreateNamedAsync(owner, $"Massage {token}");

        var page = await ListAsync(owner, $"/api/services?search=Hair");
        var blank = await ListAsync(owner, "/api/services?search=%20%20");

        page.Items.Should().ContainSingle().Which.Id.Should().Be(match.Id);
        page.TotalCount.Should().Be(1);
        blank.TotalCount.Should().Be(2);
    }

    // ---------- GET /api/services/all (public catalog for any authenticated user) ----------

    [Fact]
    public async Task GetAllServices_AsCustomer_ShouldListActiveServicesOfActiveCompaniesOnly()
    {
        var owner = await TestApi.RegisterOwnerAsync(_factory);
        var inactiveCompanyOwner = await TestApi.RegisterOwnerAsync(_factory);
        var customer = await TestApi.RegisterCustomerAsync(_factory);
        var visible = await TestApi.CreateServiceAsync(owner);
        var removed = await TestApi.CreateServiceAsync(owner);
        await DeactivateAsync(owner, removed);
        var hiddenByCompany = await TestApi.CreateServiceAsync(inactiveCompanyOwner);
        await TestApi.DeactivateCompanyAsync(_factory, hiddenByCompany.CompanyId);

        var page = await ListAsync(customer, "/api/services/all?pageSize=1000");

        var ids = page.Items.Select(s => s.Id).ToList();
        ids.Should().Contain(visible.Id);
        ids.Should().NotContain(removed.Id);
        ids.Should().NotContain(hiddenByCompany.Id);
        page.Items.Single(s => s.Id == visible.Id).Should().BeEquivalentTo(visible);
    }

    [Fact]
    public async Task GetAllServices_AsOwner_ShouldAlsoIncludeOtherCompaniesServices()
    {
        var owner = await TestApi.RegisterOwnerAsync(_factory);
        var otherOwner = await TestApi.RegisterOwnerAsync(_factory);
        var theirs = await TestApi.CreateServiceAsync(otherOwner);

        var page = await ListAsync(owner, "/api/services/all?pageSize=1000");

        page.Items.Select(s => s.Id).Should().Contain(theirs.Id);
    }

    [Fact]
    public async Task GetAllServices_ShouldPaginate()
    {
        var owner = await TestApi.RegisterOwnerAsync(_factory);
        var customer = await TestApi.RegisterCustomerAsync(_factory);
        await TestApi.CreateServiceAsync(owner);
        await TestApi.CreateServiceAsync(owner);

        var page = await ListAsync(customer, "/api/services/all?page=1&pageSize=1");

        page.Items.Should().HaveCount(1);
        page.PageSize.Should().Be(1);
        page.TotalCount.Should().BeGreaterThanOrEqualTo(2);
        page.TotalPages.Should().Be(page.TotalCount);
    }

    // ---------- GET /api/services/{id} ----------

    [Fact]
    public async Task GetService_Unknown_ShouldReturnNotFound()
    {
        var customer = await TestApi.RegisterCustomerAsync(_factory);

        var response = await customer.Client.GetAsync($"/api/services/{UnknownId}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetService_WithMalformedId_ShouldReturnBadRequest()
    {
        var customer = await TestApi.RegisterCustomerAsync(_factory);

        var response = await customer.Client.GetAsync("/api/services/not-a-guid");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetService_ActiveService_ShouldBeVisibleToCustomersAndOtherOwners()
    {
        var owner = await TestApi.RegisterOwnerAsync(_factory);
        var otherOwner = await TestApi.RegisterOwnerAsync(_factory);
        var customer = await TestApi.RegisterCustomerAsync(_factory);
        var service = await TestApi.CreateServiceAsync(owner, durationMinutes: 45);

        (await GetOkAsync(customer, service.Id)).Should().BeEquivalentTo(service);
        (await GetOkAsync(otherOwner, service.Id)).Should().BeEquivalentTo(service);
        (await GetOkAsync(owner, service.Id)).Should().BeEquivalentTo(service);
    }

    [Fact]
    public async Task GetService_InactiveService_ShouldBeVisibleOnlyToItsOwnCompany()
    {
        var owner = await TestApi.RegisterOwnerAsync(_factory);
        var otherOwner = await TestApi.RegisterOwnerAsync(_factory);
        var customer = await TestApi.RegisterCustomerAsync(_factory);
        var service = await TestApi.CreateServiceAsync(owner);
        await DeactivateAsync(owner, service);

        var own = await GetOkAsync(owner, service.Id);
        var customerResponse = await customer.Client.GetAsync($"/api/services/{service.Id}");
        var otherOwnerResponse = await otherOwner.Client.GetAsync($"/api/services/{service.Id}");

        own.IsActive.Should().BeFalse();
        customerResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        otherOwnerResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetService_OfInactiveCompany_ShouldBeVisibleOnlyToItsOwner()
    {
        var owner = await TestApi.RegisterOwnerAsync(_factory);
        var customer = await TestApi.RegisterCustomerAsync(_factory);
        var service = await TestApi.CreateServiceAsync(owner);
        await TestApi.DeactivateCompanyAsync(_factory, service.CompanyId);

        var own = await owner.Client.GetAsync($"/api/services/{service.Id}");
        var customerResponse = await customer.Client.GetAsync($"/api/services/{service.Id}");

        own.StatusCode.Should().Be(HttpStatusCode.OK);
        customerResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ---------- POST /api/services ----------

    [Fact]
    public async Task CreateService_AsCustomer_ShouldReturnForbidden()
    {
        var customer = await TestApi.RegisterCustomerAsync(_factory);

        var response = await customer.Client.PostAsJsonAsync(
            "/api/services", new ServiceRequest("Haircut", null, 30, 10m));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateService_AsOwnerWithoutCompany_ShouldReturnForbidden()
    {
        var owner = await TestApi.RegisterOwnerAsync(_factory);
        await TestApi.DetachFromCompanyAsync(_factory, owner.UserId);

        var response = await owner.Client.PostAsJsonAsync(
            "/api/services", new ServiceRequest("Haircut", null, 30, 10m));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateService_AsOwner_ShouldCreateActiveServiceInOwnCompany()
    {
        var owner = await TestApi.RegisterOwnerAsync(_factory);

        var response = await owner.Client.PostAsJsonAsync(
            "/api/services", new ServiceRequest("Haircut", "Short and neat", 45, 25.5m));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();
        var created = (await response.Content.ReadFromJsonAsync<ServiceResponse>())!;
        created.Id.Should().NotBeEmpty();
        created.Name.Should().Be("Haircut");
        created.Description.Should().Be("Short and neat");
        created.DurationMinutes.Should().Be(45);
        created.Price.Should().Be(25.5m);
        created.IsActive.Should().BeTrue();
        created.CompanyName.Should().NotBeNullOrEmpty();
        (await ListAsync(owner, "/api/services")).Items.Single().CompanyId.Should().Be(created.CompanyId);
    }

    [Fact]
    public async Task CreateService_WithoutDescription_ShouldSucceed()
    {
        var owner = await TestApi.RegisterOwnerAsync(_factory);

        var response = await owner.Client.PostAsJsonAsync(
            "/api/services", new ServiceRequest("Haircut", null, 30, 0m));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        (await response.Content.ReadFromJsonAsync<ServiceResponse>())!.Description.Should().BeNull();
    }

    [Theory]
    [InlineData("", 30, 10, "Name")]
    [InlineData("Haircut", 0, 10, "DurationMinutes")]
    [InlineData("Haircut", 481, 10, "DurationMinutes")]
    [InlineData("Haircut", 30, -1, "Price")]
    [InlineData("Haircut", 30, 100000, "Price")]
    public async Task CreateService_WithInvalidInput_ShouldReturnBadRequest(
        string name, int duration, decimal price, string invalidField)
    {
        var owner = await TestApi.RegisterOwnerAsync(_factory);

        var response = await owner.Client.PostAsJsonAsync(
            "/api/services", new ServiceRequest(name, null, duration, price));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync()).Should().Contain(invalidField);
    }

    [Fact]
    public async Task CreateService_WithTooLongNameOrDescription_ShouldReturnBadRequest()
    {
        var owner = await TestApi.RegisterOwnerAsync(_factory);

        var longName = await owner.Client.PostAsJsonAsync(
            "/api/services", new ServiceRequest(new string('n', 101), null, 30, 10m));
        var longDescription = await owner.Client.PostAsJsonAsync(
            "/api/services", new ServiceRequest("Haircut", new string('d', 501), 30, 10m));

        longName.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        longDescription.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateService_WithMalformedBody_ShouldReturnBadRequest()
    {
        var owner = await TestApi.RegisterOwnerAsync(_factory);

        var response = await owner.Client.PostAsync(
            "/api/services", new StringContent("{ not json", System.Text.Encoding.UTF8, "application/json"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ---------- PUT /api/services/{id} ----------

    [Fact]
    public async Task UpdateService_AsCustomer_ShouldReturnForbidden()
    {
        var owner = await TestApi.RegisterOwnerAsync(_factory);
        var customer = await TestApi.RegisterCustomerAsync(_factory);
        var service = await TestApi.CreateServiceAsync(owner);

        var response = await UpdateAsync(customer, service.Id, new ServiceUpdateRequest("Hacked", null, null, null, null));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await GetOkAsync(owner, service.Id)).Name.Should().Be(service.Name);
    }

    [Fact]
    public async Task UpdateService_AsOwnerWithoutCompany_ShouldReturnForbidden()
    {
        var owner = await TestApi.RegisterOwnerAsync(_factory);
        var service = await TestApi.CreateServiceAsync(owner);
        await TestApi.DetachFromCompanyAsync(_factory, owner.UserId);

        var response = await UpdateAsync(owner, service.Id, new ServiceUpdateRequest("Renamed", null, null, null, null));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UpdateService_OfAnotherCompany_ShouldReturnNotFoundAndLeaveItUntouched()
    {
        var owner = await TestApi.RegisterOwnerAsync(_factory);
        var attacker = await TestApi.RegisterOwnerAsync(_factory);
        var service = await TestApi.CreateServiceAsync(owner);

        var response = await UpdateAsync(
            attacker, service.Id, new ServiceUpdateRequest("Hacked", "x", 5, 1m, false));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await GetOkAsync(owner, service.Id)).Should().BeEquivalentTo(service);
    }

    [Fact]
    public async Task UpdateService_Unknown_ShouldReturnNotFound()
    {
        var owner = await TestApi.RegisterOwnerAsync(_factory);

        var response = await UpdateAsync(owner, UnknownId, new ServiceUpdateRequest("Renamed", null, null, null, null));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateService_WithAllFields_ShouldReplaceThemAndReturnCompanyData()
    {
        var owner = await TestApi.RegisterOwnerAsync(_factory);
        var service = await TestApi.CreateServiceAsync(owner);

        var response = await UpdateAsync(
            owner, service.Id, new ServiceUpdateRequest("Renamed", "New description", 90, 42m, false));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = (await response.Content.ReadFromJsonAsync<ServiceResponse>())!;
        updated.Should().BeEquivalentTo(new ServiceResponse(
            service.Id, "Renamed", "New description", 90, 42m, false,
            service.CompanyId, service.CompanyName, service.CompanyDescription));
        (await GetOkAsync(owner, service.Id)).Should().BeEquivalentTo(updated);
    }

    [Fact]
    public async Task UpdateService_WithOnlyNullFields_ShouldChangeNothing()
    {
        var owner = await TestApi.RegisterOwnerAsync(_factory);
        var service = await TestApi.CreateServiceAsync(owner);

        var response = await UpdateAsync(owner, service.Id, new ServiceUpdateRequest(null, null, null, null, null));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadFromJsonAsync<ServiceResponse>())!.Should().BeEquivalentTo(service);
    }

    [Fact]
    public async Task UpdateService_WithEmptyName_ShouldKeepTheCurrentName()
    {
        var owner = await TestApi.RegisterOwnerAsync(_factory);
        var service = await TestApi.CreateServiceAsync(owner);

        var response = await UpdateAsync(owner, service.Id, new ServiceUpdateRequest("", null, null, null, null));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadFromJsonAsync<ServiceResponse>())!.Name.Should().Be(service.Name);
    }

    [Fact]
    public async Task UpdateService_WithEmptyDescription_ShouldClearIt()
    {
        var owner = await TestApi.RegisterOwnerAsync(_factory);
        var service = await TestApi.CreateServiceAsync(owner);

        var response = await UpdateAsync(owner, service.Id, new ServiceUpdateRequest(null, "", null, null, null));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadFromJsonAsync<ServiceResponse>())!.Description.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateService_CanReactivateADeletedService()
    {
        var owner = await TestApi.RegisterOwnerAsync(_factory);
        var service = await TestApi.CreateServiceAsync(owner);
        await DeactivateAsync(owner, service);

        var response = await UpdateAsync(owner, service.Id, new ServiceUpdateRequest(null, null, null, null, true));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await ListAsync(owner, "/api/services")).Items.Select(s => s.Id).Should().Contain(service.Id);
    }

    [Theory]
    [InlineData(0, null)]
    [InlineData(481, null)]
    [InlineData(null, -1)]
    public async Task UpdateService_WithOutOfRangeValues_ShouldReturnBadRequest(int? duration, int? price)
    {
        var owner = await TestApi.RegisterOwnerAsync(_factory);
        var service = await TestApi.CreateServiceAsync(owner);

        var response = await UpdateAsync(
            owner, service.Id, new ServiceUpdateRequest(null, null, duration, price, null));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await GetOkAsync(owner, service.Id)).Should().BeEquivalentTo(service);
    }

    // ---------- DELETE /api/services/{id} ----------

    [Fact]
    public async Task DeleteService_AsCustomer_ShouldReturnForbidden()
    {
        var owner = await TestApi.RegisterOwnerAsync(_factory);
        var customer = await TestApi.RegisterCustomerAsync(_factory);
        var service = await TestApi.CreateServiceAsync(owner);

        var response = await customer.Client.DeleteAsync($"/api/services/{service.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await GetOkAsync(owner, service.Id)).IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteService_AsOwnerWithoutCompany_ShouldReturnForbidden()
    {
        var owner = await TestApi.RegisterOwnerAsync(_factory);
        var service = await TestApi.CreateServiceAsync(owner);
        await TestApi.DetachFromCompanyAsync(_factory, owner.UserId);

        var response = await owner.Client.DeleteAsync($"/api/services/{service.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DeleteService_OfAnotherCompany_ShouldReturnNotFoundAndKeepItActive()
    {
        var owner = await TestApi.RegisterOwnerAsync(_factory);
        var attacker = await TestApi.RegisterOwnerAsync(_factory);
        var service = await TestApi.CreateServiceAsync(owner);

        var response = await attacker.Client.DeleteAsync($"/api/services/{service.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await GetOkAsync(owner, service.Id)).IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteService_Unknown_ShouldReturnNotFound()
    {
        var owner = await TestApi.RegisterOwnerAsync(_factory);

        var response = await owner.Client.DeleteAsync($"/api/services/{UnknownId}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteService_ShouldSoftDeleteAndBeIdempotent()
    {
        var owner = await TestApi.RegisterOwnerAsync(_factory);
        var customer = await TestApi.RegisterCustomerAsync(_factory);
        var service = await TestApi.CreateServiceAsync(owner);

        var first = await owner.Client.DeleteAsync($"/api/services/{service.Id}");
        var second = await owner.Client.DeleteAsync($"/api/services/{service.Id}");

        first.StatusCode.Should().Be(HttpStatusCode.NoContent);
        second.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await GetOkAsync(owner, service.Id)).IsActive.Should().BeFalse();
        (await ListAsync(owner, "/api/services")).Items.Should().BeEmpty();
        (await ListAsync(customer, "/api/services/all?pageSize=1000")).Items
            .Select(s => s.Id).Should().NotContain(service.Id);
    }
}
