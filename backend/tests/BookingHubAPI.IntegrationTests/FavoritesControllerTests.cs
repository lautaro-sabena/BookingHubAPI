using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BookingHubAPI.Application.DTOs;
using BookingHubAPI.IntegrationTests.Support;
using FluentAssertions;
using Xunit;

namespace BookingHubAPI.IntegrationTests;

/// <summary>
/// Characterization tests for /api/favorites. They pin the observable HTTP behavior (status codes
/// and body shapes) so the controller can be refactored without changing it. Each test registers
/// its own owners/customers, so tests never interfere with each other.
/// </summary>
public class FavoritesControllerTests : IClassFixture<BookingApiFactory>
{
    private static readonly Guid UnknownId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    private readonly BookingApiFactory _factory;

    public FavoritesControllerTests(BookingApiFactory factory)
    {
        _factory = factory;
    }

    private static async Task<List<FavoriteDto>> ListAsync(TestUser customer)
    {
        var response = await customer.Client.GetAsync("/api/favorites");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<List<FavoriteDto>>())!;
    }

    private static Task<HttpResponseMessage> AddAsync(TestUser customer, Guid serviceId) =>
        customer.Client.PostAsync($"/api/favorites/{serviceId}", null);

    // ---------- authentication and roles ----------

    [Theory]
    [InlineData("GET", "/api/favorites")]
    [InlineData("POST", "/api/favorites/00000000-0000-0000-0000-000000000001")]
    [InlineData("DELETE", "/api/favorites/00000000-0000-0000-0000-000000000001")]
    [InlineData("GET", "/api/favorites/00000000-0000-0000-0000-000000000001/check")]
    public async Task AnyEndpoint_WithoutToken_ShouldReturnUnauthorized(string method, string url)
    {
        var client = _factory.CreateClient();

        var response = await client.SendAsync(new HttpRequestMessage(new HttpMethod(method), url));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData("GET", "/api/favorites")]
    [InlineData("POST", "/api/favorites/00000000-0000-0000-0000-000000000001")]
    [InlineData("DELETE", "/api/favorites/00000000-0000-0000-0000-000000000001")]
    [InlineData("GET", "/api/favorites/00000000-0000-0000-0000-000000000001/check")]
    public async Task AnyEndpoint_AsOwner_ShouldReturnForbidden(string method, string url)
    {
        var owner = await TestApi.RegisterOwnerAsync(_factory);

        var response = await owner.Client.SendAsync(new HttpRequestMessage(new HttpMethod(method), url));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ---------- GET /api/favorites ----------

    [Fact]
    public async Task GetFavorites_WithNone_ShouldReturnEmptyArray()
    {
        var customer = await TestApi.RegisterCustomerAsync(_factory);

        var response = await customer.Client.GetAsync("/api/favorites");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).Should().Be("[]");
    }

    [Fact]
    public async Task GetFavorites_ShouldReturnEveryFieldOfTheFavoriteDto()
    {
        var owner = await TestApi.RegisterOwnerAsync(_factory);
        var created = await owner.Client.PostAsJsonAsync("/api/services", new ServiceRequest("Deep tissue", "Relaxing", 45, 32.5m));
        var service = (await created.Content.ReadFromJsonAsync<ServiceResponse>())!;
        var customer = await TestApi.RegisterCustomerAsync(_factory);
        var added = (await (await AddAsync(customer, service.Id)).Content.ReadFromJsonAsync<FavoriteDto>())!;

        var response = await customer.Client.GetAsync("/api/favorites");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var items = (await response.Content.ReadFromJsonAsync<JsonElement>()).EnumerateArray().ToList();
        items.Should().ContainSingle();
        var item = items[0];
        item.EnumerateObject().Select(p => p.Name).Should().BeEquivalentTo(
            "id", "serviceId", "serviceName", "serviceDescription", "durationMinutes", "price", "companyId", "companyName");
        item.GetProperty("id").GetGuid().Should().Be(added.Id).And.NotBe(Guid.Empty);
        item.GetProperty("serviceId").GetGuid().Should().Be(service.Id);
        item.GetProperty("serviceName").GetString().Should().Be("Deep tissue");
        item.GetProperty("serviceDescription").GetString().Should().Be("Relaxing");
        item.GetProperty("durationMinutes").GetInt32().Should().Be(45);
        item.GetProperty("price").GetDecimal().Should().Be(32.5m);
        item.GetProperty("companyId").GetGuid().Should().Be(service.CompanyId);
        item.GetProperty("companyName").GetString().Should().Be($"{owner.Email}'s Company");
    }

    [Fact]
    public async Task GetFavorites_ShouldReturnOnlyTheCallersFavorites()
    {
        var owner = await TestApi.RegisterOwnerAsync(_factory);
        var serviceOne = await TestApi.CreateServiceAsync(owner);
        var serviceTwo = await TestApi.CreateServiceAsync(owner);
        var customerA = await TestApi.RegisterCustomerAsync(_factory);
        var customerB = await TestApi.RegisterCustomerAsync(_factory);
        await AddAsync(customerA, serviceOne.Id);
        await AddAsync(customerB, serviceTwo.Id);

        var favoritesA = await ListAsync(customerA);
        var favoritesB = await ListAsync(customerB);

        favoritesA.Select(f => f.ServiceId).Should().Equal(serviceOne.Id);
        favoritesB.Select(f => f.ServiceId).Should().Equal(serviceTwo.Id);
    }

    // ---------- POST /api/favorites/{serviceId} ----------

    [Fact]
    public async Task AddFavorite_ShouldReturnOkWithTheFavoriteDto()
    {
        var owner = await TestApi.RegisterOwnerAsync(_factory);
        var created = await owner.Client.PostAsJsonAsync("/api/services", new ServiceRequest("Cut", "Short hair", 30, 12m));
        var service = (await created.Content.ReadFromJsonAsync<ServiceResponse>())!;
        var customer = await TestApi.RegisterCustomerAsync(_factory);

        var response = await AddAsync(customer, service.Id);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.EnumerateObject().Select(p => p.Name).Should().BeEquivalentTo(
            "id", "serviceId", "serviceName", "serviceDescription", "durationMinutes", "price", "companyId", "companyName");
        body.GetProperty("id").GetGuid().Should().NotBeEmpty();
        body.GetProperty("serviceId").GetGuid().Should().Be(service.Id);
        body.GetProperty("serviceName").GetString().Should().Be("Cut");
        body.GetProperty("serviceDescription").GetString().Should().Be("Short hair");
        body.GetProperty("durationMinutes").GetInt32().Should().Be(30);
        body.GetProperty("price").GetDecimal().Should().Be(12m);
        body.GetProperty("companyId").GetGuid().Should().Be(service.CompanyId);
        body.GetProperty("companyName").GetString().Should().Be($"{owner.Email}'s Company");
    }

    [Fact]
    public async Task AddFavorite_ForServiceWithoutDescription_ShouldReturnNullDescription()
    {
        var owner = await TestApi.RegisterOwnerAsync(_factory);
        var created = await owner.Client.PostAsJsonAsync("/api/services", new ServiceRequest("Bare", null, 30, 5m));
        var service = (await created.Content.ReadFromJsonAsync<ServiceResponse>())!;
        var customer = await TestApi.RegisterCustomerAsync(_factory);

        var response = await AddAsync(customer, service.Id);

        (await response.Content.ReadFromJsonAsync<FavoriteDto>())!.ServiceDescription.Should().BeNull();
    }

    [Fact]
    public async Task AddFavorite_ForUnknownService_ShouldReturnNotFoundWithMessage()
    {
        var customer = await TestApi.RegisterCustomerAsync(_factory);

        var response = await AddAsync(customer, UnknownId);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await response.Content.ReadAsStringAsync()).Should().Contain("Service not found");
    }

    [Fact]
    public async Task AddFavorite_ForInactiveService_ShouldReturnNotFound()
    {
        var owner = await TestApi.RegisterOwnerAsync(_factory);
        var service = await TestApi.CreateServiceAsync(owner);
        (await owner.Client.DeleteAsync($"/api/services/{service.Id}")).EnsureSuccessStatusCode();
        var customer = await TestApi.RegisterCustomerAsync(_factory);

        var response = await AddAsync(customer, service.Id);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "an inactive service must stay hidden, as GET /api/services/{id} does");
        (await customer.Client.GetFromJsonAsync<List<FavoriteDto>>("/api/favorites")).Should().BeEmpty();
    }

    [Fact]
    public async Task AddFavorite_ForServiceOfInactiveCompany_ShouldReturnNotFound()
    {
        var owner = await TestApi.RegisterOwnerAsync(_factory);
        var service = await TestApi.CreateServiceAsync(owner);
        await TestApi.DeactivateCompanyAsync(_factory, service.CompanyId);
        var customer = await TestApi.RegisterCustomerAsync(_factory);

        var response = await AddAsync(customer, service.Id);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "a service of an inactive company must stay hidden, as GET /api/services/{id} does");
        (await customer.Client.GetFromJsonAsync<List<FavoriteDto>>("/api/favorites")).Should().BeEmpty();
    }

    [Fact]
    public async Task AddFavorite_Twice_ShouldReturnBadRequestWithMessageAndKeepASingleFavorite()
    {
        var owner = await TestApi.RegisterOwnerAsync(_factory);
        var service = await TestApi.CreateServiceAsync(owner);
        var customer = await TestApi.RegisterCustomerAsync(_factory);
        await AddAsync(customer, service.Id);

        var response = await AddAsync(customer, service.Id);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync()).Should().Contain("Service already in favorites");
        (await ListAsync(customer)).Should().ContainSingle();
    }

    [Fact]
    public async Task AddFavorite_ByTwoCustomersForTheSameService_ShouldBothSucceed()
    {
        var owner = await TestApi.RegisterOwnerAsync(_factory);
        var service = await TestApi.CreateServiceAsync(owner);
        var customerA = await TestApi.RegisterCustomerAsync(_factory);
        var customerB = await TestApi.RegisterCustomerAsync(_factory);

        var first = await AddAsync(customerA, service.Id);
        var second = await AddAsync(customerB, service.Id);

        first.StatusCode.Should().Be(HttpStatusCode.OK);
        second.StatusCode.Should().Be(HttpStatusCode.OK);
        (await first.Content.ReadFromJsonAsync<FavoriteDto>())!.Id
            .Should().NotBe((await second.Content.ReadFromJsonAsync<FavoriteDto>())!.Id);
    }

    [Fact]
    public async Task AddFavorite_WithMalformedServiceId_ShouldReturnBadRequest()
    {
        var customer = await TestApi.RegisterCustomerAsync(_factory);

        var response = await customer.Client.PostAsync("/api/favorites/not-a-guid", null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ---------- DELETE /api/favorites/{serviceId} ----------

    [Fact]
    public async Task RemoveFavorite_ShouldReturnNoContentAndRemoveIt()
    {
        var owner = await TestApi.RegisterOwnerAsync(_factory);
        var service = await TestApi.CreateServiceAsync(owner);
        var customer = await TestApi.RegisterCustomerAsync(_factory);
        await AddAsync(customer, service.Id);

        var response = await customer.Client.DeleteAsync($"/api/favorites/{service.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await ListAsync(customer)).Should().BeEmpty();
    }

    [Fact]
    public async Task RemoveFavorite_Twice_ShouldReturnNotFoundTheSecondTime()
    {
        var owner = await TestApi.RegisterOwnerAsync(_factory);
        var service = await TestApi.CreateServiceAsync(owner);
        var customer = await TestApi.RegisterCustomerAsync(_factory);
        await AddAsync(customer, service.Id);
        await customer.Client.DeleteAsync($"/api/favorites/{service.Id}");

        var response = await customer.Client.DeleteAsync($"/api/favorites/{service.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await response.Content.ReadAsStringAsync()).Should().Contain("Favorite not found");
    }

    [Fact]
    public async Task RemoveFavorite_ForUnknownService_ShouldReturnNotFound()
    {
        var customer = await TestApi.RegisterCustomerAsync(_factory);

        var response = await customer.Client.DeleteAsync($"/api/favorites/{UnknownId}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await response.Content.ReadAsStringAsync()).Should().Contain("Favorite not found");
    }

    [Fact]
    public async Task RemoveFavorite_OfAnotherCustomer_ShouldReturnNotFoundAndLeaveItIntact()
    {
        var owner = await TestApi.RegisterOwnerAsync(_factory);
        var service = await TestApi.CreateServiceAsync(owner);
        var customerA = await TestApi.RegisterCustomerAsync(_factory);
        var customerB = await TestApi.RegisterCustomerAsync(_factory);
        await AddAsync(customerA, service.Id);

        var response = await customerB.Client.DeleteAsync($"/api/favorites/{service.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await ListAsync(customerA)).Should().ContainSingle().Which.ServiceId.Should().Be(service.Id);
    }

    [Fact]
    public async Task RemoveFavorite_ShouldOnlyRemoveTheCallersOwnFavoriteOfTheSameService()
    {
        var owner = await TestApi.RegisterOwnerAsync(_factory);
        var service = await TestApi.CreateServiceAsync(owner);
        var customerA = await TestApi.RegisterCustomerAsync(_factory);
        var customerB = await TestApi.RegisterCustomerAsync(_factory);
        await AddAsync(customerA, service.Id);
        await AddAsync(customerB, service.Id);

        var response = await customerB.Client.DeleteAsync($"/api/favorites/{service.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await ListAsync(customerB)).Should().BeEmpty();
        (await ListAsync(customerA)).Should().ContainSingle();
    }

    // ---------- GET /api/favorites/{serviceId}/check ----------

    [Fact]
    public async Task CheckFavorite_ShouldReturnTrueOnlyForTheCustomerWhoFavoritedIt()
    {
        var owner = await TestApi.RegisterOwnerAsync(_factory);
        var service = await TestApi.CreateServiceAsync(owner);
        var customerA = await TestApi.RegisterCustomerAsync(_factory);
        var customerB = await TestApi.RegisterCustomerAsync(_factory);
        await AddAsync(customerA, service.Id);

        var forA = await customerA.Client.GetAsync($"/api/favorites/{service.Id}/check");
        var forB = await customerB.Client.GetAsync($"/api/favorites/{service.Id}/check");

        forA.StatusCode.Should().Be(HttpStatusCode.OK);
        (await forA.Content.ReadAsStringAsync()).Should().Be("true");
        forB.StatusCode.Should().Be(HttpStatusCode.OK);
        (await forB.Content.ReadAsStringAsync()).Should().Be("false");
    }

    [Fact]
    public async Task CheckFavorite_ForUnknownService_ShouldReturnFalse()
    {
        var customer = await TestApi.RegisterCustomerAsync(_factory);

        var response = await customer.Client.GetAsync($"/api/favorites/{UnknownId}/check");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).Should().Be("false");
    }

    [Fact]
    public async Task CheckFavorite_AfterRemoval_ShouldReturnFalse()
    {
        var owner = await TestApi.RegisterOwnerAsync(_factory);
        var service = await TestApi.CreateServiceAsync(owner);
        var customer = await TestApi.RegisterCustomerAsync(_factory);
        await AddAsync(customer, service.Id);
        await customer.Client.DeleteAsync($"/api/favorites/{service.Id}");

        var response = await customer.Client.GetAsync($"/api/favorites/{service.Id}/check");

        (await response.Content.ReadAsStringAsync()).Should().Be("false");
    }
}
