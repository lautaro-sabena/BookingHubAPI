using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BookingHubAPI.Application.DTOs;
using BookingHubAPI.IntegrationTests.Support;
using FluentAssertions;
using Xunit;

namespace BookingHubAPI.IntegrationTests;

/// <summary>
/// Characterization tests for /api/workinghours. They pin the observable HTTP behavior (status
/// codes and body shapes) so the controller can be refactored without changing it. Each test
/// registers its own owners, so tests never interfere with each other.
/// </summary>
public class WorkingHoursControllerTests : IClassFixture<BookingApiFactory>
{
    private static readonly TimeSpan DefaultStart = new(9, 0, 0);
    private static readonly TimeSpan DefaultEnd = new(17, 0, 0);

    private readonly BookingApiFactory _factory;

    public WorkingHoursControllerTests(BookingApiFactory factory)
    {
        _factory = factory;
    }

    // ---------- helpers ----------

    private static WorkingHoursRequest Day(DayOfWeek day, int startHour, int endHour, bool isActive = true) =>
        new(day, new TimeSpan(startHour, 0, 0), new TimeSpan(endHour, 0, 0), isActive);

    private static async Task<List<WorkingHoursResponse>> GetHoursAsync(TestUser owner)
    {
        var response = await owner.Client.GetAsync("/api/workinghours");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<List<WorkingHoursResponse>>())!;
    }

    private static Task<HttpResponseMessage> PutHoursAsync(TestUser user, params WorkingHoursRequest[] requests) =>
        user.Client.PutAsJsonAsync("/api/workinghours", requests.ToList());

    private static async Task PutHoursOkAsync(TestUser owner, params WorkingHoursRequest[] requests)
    {
        var response = await PutHoursAsync(owner, requests);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ---------- authentication and roles ----------

    [Theory]
    [InlineData("GET")]
    [InlineData("PUT")]
    public async Task AnyEndpoint_WithoutToken_ShouldReturnUnauthorized(string method)
    {
        var client = _factory.CreateClient();

        var response = await client.SendAsync(new HttpRequestMessage(new HttpMethod(method), "/api/workinghours"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData("GET")]
    [InlineData("PUT")]
    public async Task AnyEndpoint_AsCustomer_ShouldReturnForbidden(string method)
    {
        var customer = await TestApi.RegisterCustomerAsync(_factory);
        var request = new HttpRequestMessage(new HttpMethod(method), "/api/workinghours");
        if (method == "PUT")
        {
            request.Content = JsonContent.Create(new List<WorkingHoursRequest> { Day(DayOfWeek.Monday, 8, 12) });
        }

        var response = await customer.Client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Theory]
    [InlineData("GET")]
    [InlineData("PUT")]
    public async Task AnyEndpoint_AsOwnerWithoutCompany_ShouldReturnForbidden(string method)
    {
        var owner = await TestApi.RegisterOwnerAsync(_factory);
        await TestApi.DetachFromCompanyAsync(_factory, owner.UserId);
        var request = new HttpRequestMessage(new HttpMethod(method), "/api/workinghours");
        if (method == "PUT")
        {
            request.Content = JsonContent.Create(new List<WorkingHoursRequest> { Day(DayOfWeek.Monday, 8, 12) });
        }

        var response = await owner.Client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ---------- GET ----------

    [Fact]
    public async Task GetWorkingHours_WhenNoneConfigured_ShouldReturnAllSevenDaysInactiveWithDefaultTimes()
    {
        var owner = await TestApi.RegisterOwnerAsync(_factory);

        var hours = await GetHoursAsync(owner);

        hours.Select(h => h.DayOfWeek).Should().Equal(
            DayOfWeek.Sunday, DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday,
            DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday);
        hours.Should().OnlyContain(h => h.StartTime == DefaultStart && h.EndTime == DefaultEnd && !h.IsActive);
    }

    [Fact]
    public async Task GetWorkingHours_ShouldSerializeDaysAsNumbersAndTimesAsClockStrings()
    {
        var owner = await TestApi.RegisterOwnerAsync(_factory);
        await PutHoursOkAsync(owner, Day(DayOfWeek.Monday, 8, 12));

        var response = await owner.Client.GetAsync("/api/workinghours");
        var monday = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement[1];

        monday.GetProperty("dayOfWeek").GetInt32().Should().Be(1);
        monday.GetProperty("startTime").GetString().Should().Be("08:00:00");
        monday.GetProperty("endTime").GetString().Should().Be("12:00:00");
        monday.GetProperty("isActive").GetBoolean().Should().BeTrue();
    }

    // ---------- PUT ----------

    [Fact]
    public async Task UpdateWorkingHours_ShouldPersistActiveDaysAndLeaveTheRestAtDefaults()
    {
        var owner = await TestApi.RegisterOwnerAsync(_factory);

        await PutHoursOkAsync(owner, Day(DayOfWeek.Monday, 8, 12), Day(DayOfWeek.Friday, 10, 20));

        var hours = await GetHoursAsync(owner);
        hours.Should().HaveCount(7);
        hours[(int)DayOfWeek.Monday].Should().Be(new WorkingHoursResponse(
            DayOfWeek.Monday, new TimeSpan(8, 0, 0), new TimeSpan(12, 0, 0), true));
        hours[(int)DayOfWeek.Friday].Should().Be(new WorkingHoursResponse(
            DayOfWeek.Friday, new TimeSpan(10, 0, 0), new TimeSpan(20, 0, 0), true));
        hours.Where(h => h.DayOfWeek is not (DayOfWeek.Monday or DayOfWeek.Friday))
            .Should().OnlyContain(h => !h.IsActive && h.StartTime == DefaultStart && h.EndTime == DefaultEnd);
    }

    [Fact]
    public async Task UpdateWorkingHours_ShouldReplaceThePreviousSchedule()
    {
        var owner = await TestApi.RegisterOwnerAsync(_factory);
        await PutHoursOkAsync(owner, Day(DayOfWeek.Monday, 8, 12), Day(DayOfWeek.Tuesday, 8, 12));

        await PutHoursOkAsync(owner, Day(DayOfWeek.Tuesday, 13, 18));

        var hours = await GetHoursAsync(owner);
        hours[(int)DayOfWeek.Monday].IsActive.Should().BeFalse();
        hours[(int)DayOfWeek.Tuesday].Should().Be(new WorkingHoursResponse(
            DayOfWeek.Tuesday, new TimeSpan(13, 0, 0), new TimeSpan(18, 0, 0), true));
    }

    [Fact]
    public async Task UpdateWorkingHours_WithEmptyList_ShouldClearTheSchedule()
    {
        var owner = await TestApi.RegisterOwnerAsync(_factory);
        await PutHoursOkAsync(owner, Day(DayOfWeek.Monday, 8, 12));

        await PutHoursOkAsync(owner);

        (await GetHoursAsync(owner)).Should().OnlyContain(h => !h.IsActive);
    }

    [Fact]
    public async Task UpdateWorkingHours_WithInactiveDay_ShouldNotStoreItsTimes()
    {
        // CURRENT BEHAVIOR (bug): inactive entries are dropped instead of stored, so the custom
        // times the owner sent for a disabled day are lost and GET reports the 09:00-17:00 defaults.
        var owner = await TestApi.RegisterOwnerAsync(_factory);

        await PutHoursOkAsync(owner, Day(DayOfWeek.Wednesday, 6, 7, isActive: false));

        var wednesday = (await GetHoursAsync(owner))[(int)DayOfWeek.Wednesday];
        wednesday.IsActive.Should().BeFalse();
        wednesday.StartTime.Should().Be(DefaultStart);
        wednesday.EndTime.Should().Be(DefaultEnd);
    }

    [Fact]
    public async Task UpdateWorkingHours_ResponseBody_ShouldBeAWrappedEmptyObjectInsteadOfTheDayList()
    {
        // CURRENT BEHAVIOR (bug): the controller returns Ok(await GetWorkingHours()), which nests the
        // ActionResult<T> of the GET call inside the response instead of its value, so the body is
        // {"result":{},"value":null} rather than the array of seven days the declared type suggests.
        var owner = await TestApi.RegisterOwnerAsync(_factory);

        var response = await PutHoursAsync(owner, Day(DayOfWeek.Monday, 8, 12));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
        body.ValueKind.Should().Be(JsonValueKind.Object);
        body.GetProperty("result").ValueKind.Should().Be(JsonValueKind.Object);
        body.GetProperty("value").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task UpdateWorkingHours_WithStartAfterEnd_ShouldStillStoreIt()
    {
        // CURRENT BEHAVIOR (bug): no validation of the time range, so an inverted window is accepted.
        var owner = await TestApi.RegisterOwnerAsync(_factory);

        await PutHoursOkAsync(owner, Day(DayOfWeek.Thursday, 18, 8));

        (await GetHoursAsync(owner))[(int)DayOfWeek.Thursday].Should().Be(new WorkingHoursResponse(
            DayOfWeek.Thursday, new TimeSpan(18, 0, 0), new TimeSpan(8, 0, 0), true));
    }

    [Fact]
    public async Task UpdateWorkingHours_WithDuplicateDay_ShouldReportTheFirstEntry()
    {
        // CURRENT BEHAVIOR (bug): no check for duplicate days; both rows are stored and GET reads
        // the times of whichever row comes first while IsActive is true if any row is active.
        var owner = await TestApi.RegisterOwnerAsync(_factory);

        await PutHoursOkAsync(owner, Day(DayOfWeek.Monday, 8, 12), Day(DayOfWeek.Monday, 14, 18));

        (await GetHoursAsync(owner))[(int)DayOfWeek.Monday].Should().Be(new WorkingHoursResponse(
            DayOfWeek.Monday, new TimeSpan(8, 0, 0), new TimeSpan(12, 0, 0), true));
    }

    [Fact]
    public async Task UpdateWorkingHours_ShouldNotTouchAnotherCompanysSchedule()
    {
        var owner = await TestApi.RegisterOwnerAsync(_factory);
        var otherOwner = await TestApi.RegisterOwnerAsync(_factory);
        await PutHoursOkAsync(owner, Day(DayOfWeek.Monday, 8, 12));

        await PutHoursOkAsync(otherOwner, Day(DayOfWeek.Tuesday, 9, 10));

        var mine = await GetHoursAsync(owner);
        mine[(int)DayOfWeek.Monday].IsActive.Should().BeTrue();
        mine[(int)DayOfWeek.Tuesday].IsActive.Should().BeFalse();
        var theirs = await GetHoursAsync(otherOwner);
        theirs[(int)DayOfWeek.Monday].IsActive.Should().BeFalse();
        theirs[(int)DayOfWeek.Tuesday].IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateWorkingHours_WithMalformedBody_ShouldReturnBadRequest()
    {
        var owner = await TestApi.RegisterOwnerAsync(_factory);

        var response = await owner.Client.PutAsync(
            "/api/workinghours", new StringContent("{ not json", System.Text.Encoding.UTF8, "application/json"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateWorkingHours_WithOutOfRangeDay_ShouldBeAcceptedButNeverReported()
    {
        var owner = await TestApi.RegisterOwnerAsync(_factory);
        const string body = """[{"dayOfWeek":9,"startTime":"08:00:00","endTime":"12:00:00","isActive":true}]""";

        var response = await owner.Client.PutAsync(
            "/api/workinghours", new StringContent(body, System.Text.Encoding.UTF8, "application/json"));

        // CURRENT BEHAVIOR (bug): the enum is not range-checked, so day 9 is accepted and stored.
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await GetHoursAsync(owner)).Should().OnlyContain(h => !h.IsActive);
    }
}
