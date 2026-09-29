using System.Net;
using BookingHubAPI.IntegrationTests.Support;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace BookingHubAPI.IntegrationTests;

/// <summary>Every paged endpoint rejects out-of-range page / pageSize with a 400 validation problem.</summary>
public class PagingValidationTests : IClassFixture<BookingApiFactory>
{
    private readonly BookingApiFactory _factory;

    public PagingValidationTests(BookingApiFactory factory)
    {
        _factory = factory;
    }

    public static TheoryData<string, string> InvalidPaging => new()
    {
        { "page=0", "page" },
        { "page=-1", "page" },
        { "page=2147483647", "page" },
        { "pageSize=0", "pageSize" },
        { "pageSize=-5", "pageSize" },
        { "pageSize=101", "pageSize" },
    };

    [Theory]
    [MemberData(nameof(InvalidPaging))]
    public async Task OwnServices_WithInvalidPaging_ShouldReturnValidationProblem(string query, string field)
    {
        var owner = await TestApi.RegisterOwnerAsync(_factory);

        await AssertInvalidAsync(await owner.Client.GetAsync($"/api/services?{query}"), field);
    }

    [Theory]
    [MemberData(nameof(InvalidPaging))]
    public async Task PublicServices_WithInvalidPaging_ShouldReturnValidationProblem(string query, string field)
    {
        var customer = await TestApi.RegisterCustomerAsync(_factory);

        await AssertInvalidAsync(await customer.Client.GetAsync($"/api/services/all?{query}"), field);
    }

    [Theory]
    [MemberData(nameof(InvalidPaging))]
    public async Task Reservations_WithInvalidPaging_ShouldReturnValidationProblem(string query, string field)
    {
        var customer = await TestApi.RegisterCustomerAsync(_factory);

        await AssertInvalidAsync(await customer.Client.GetAsync($"/api/reservations?{query}"), field);
    }

    [Theory]
    [InlineData("/api/services/all?page=1&pageSize=100")]
    [InlineData("/api/reservations?page=1&pageSize=100")]
    public async Task PagingAtTheLimits_ShouldBeAccepted(string url)
    {
        var customer = await TestApi.RegisterCustomerAsync(_factory);

        (await customer.Client.GetAsync(url)).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private static async Task AssertInvalidAsync(HttpResponseMessage response, string field)
    {
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problem = await response.ReadProblemAsync();
        problem.Should().BeOfType<ProblemDetails>();
        problem.Extensions.Should().ContainKey("errors");
        System.Text.Json.JsonSerializer.Serialize(problem.Extensions["errors"]).Should().Contain(field);
    }
}
