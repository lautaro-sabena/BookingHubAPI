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
    public async Task UpdateWorkingHours_WithInactiveDay_ShouldKeepItsCustomTimes()
    {
        var owner = await TestApi.RegisterOwnerAsync(_factory);

        await PutHoursOkAsync(owner, Day(DayOfWeek.Wednesday, 6, 7, isActive: false));

        (await GetHoursAsync(owner))[(int)DayOfWeek.Wednesday].Should().Be(new WorkingHoursResponse(
            DayOfWeek.Wednesday, new TimeSpan(6, 0, 0), new TimeSpan(7, 0, 0), false));
    }

    [Fact]
    public async Task UpdateWorkingHours_ResponseBody_ShouldBeTheSavedSevenDaySchedule()
    {
        var owner = await TestApi.RegisterOwnerAsync(_factory);

        var response = await PutHoursAsync(owner, Day(DayOfWeek.Monday, 8, 12), Day(DayOfWeek.Wednesday, 6, 7, isActive: false));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = (await response.Content.ReadFromJsonAsync<List<WorkingHoursResponse>>())!;
        body.Should().Equal(await GetHoursAsync(owner));
        body.Should().HaveCount(7);
        body[(int)DayOfWeek.Monday].Should().Be(new WorkingHoursResponse(
            DayOfWeek.Monday, new TimeSpan(8, 0, 0), new TimeSpan(12, 0, 0), true));
    }

    [Theory]
    [InlineData(18, 8)]
    [InlineData(8, 8)]
    public async Task UpdateWorkingHours_WithActiveDayStartingNotBeforeEnd_ShouldReturnBadRequest(int start, int end)
    {
        var owner = await TestApi.RegisterOwnerAsync(_factory);
        await PutHoursOkAsync(owner, Day(DayOfWeek.Monday, 8, 12));

        var response = await PutHoursAsync(owner, Day(DayOfWeek.Thursday, start, end));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync()).Should().Contain("Thursday");
        (await GetHoursAsync(owner))[(int)DayOfWeek.Monday].IsActive.Should().BeTrue("a rejected request must not replace the schedule");
    }

    [Fact]
    public async Task UpdateWorkingHours_WithDuplicateDay_ShouldReturnBadRequest()
    {
        var owner = await TestApi.RegisterOwnerAsync(_factory);
        await PutHoursOkAsync(owner, Day(DayOfWeek.Tuesday, 8, 12));

        var response = await PutHoursAsync(owner, Day(DayOfWeek.Monday, 8, 12), Day(DayOfWeek.Monday, 14, 18));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var hours = await GetHoursAsync(owner);
        hours[(int)DayOfWeek.Tuesday].IsActive.Should().BeTrue("a rejected request must not replace the schedule");
        hours[(int)DayOfWeek.Monday].IsActive.Should().BeFalse();
    }

    [Theory]
    [InlineData("-01:00:00", "12:00:00")]
    [InlineData("08:00:00", "1.01:00:00")]   // 25:00
    public async Task UpdateWorkingHours_WithTimesOutsideTheDay_ShouldReturnBadRequestAndKeepTheSchedule(
        string start, string end)
    {
        var owner = await TestApi.RegisterOwnerAsync(_factory);
        await PutHoursOkAsync(owner, Day(DayOfWeek.Tuesday, 8, 12));
        var body = $$"""[{"dayOfWeek":1,"startTime":"{{start}}","endTime":"{{end}}","isActive":true}]""";

        var response = await owner.Client.PutAsync(
            "/api/workinghours", new StringContent(body, System.Text.Encoding.UTF8, "application/json"));

        await response.ShouldBeProblemAsync(
            HttpStatusCode.BadRequest, "Working hours must be within 00:00 and 24:00 for Monday");
        (await GetHoursAsync(owner))[(int)DayOfWeek.Tuesday].IsActive.Should().BeTrue("a rejected request must not replace the schedule");
    }

    [Fact]
    public async Task UpdateWorkingHours_WithADayRunningUntilMidnight_ShouldBeAccepted()
    {
        var owner = await TestApi.RegisterOwnerAsync(_factory);

        await PutHoursOkAsync(owner, Day(DayOfWeek.Friday, 0, 24));

        (await GetHoursAsync(owner))[(int)DayOfWeek.Friday].Should().Be(new WorkingHoursResponse(
            DayOfWeek.Friday, TimeSpan.Zero, new TimeSpan(24, 0, 0), true));
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
    public async Task UpdateWorkingHours_WithOutOfRangeDay_ShouldReturnBadRequest()
    {
        var owner = await TestApi.RegisterOwnerAsync(_factory);
        await PutHoursOkAsync(owner, Day(DayOfWeek.Tuesday, 8, 12));
        const string body = """[{"dayOfWeek":9,"startTime":"08:00:00","endTime":"12:00:00","isActive":true}]""";

        var response = await owner.Client.PutAsync(
            "/api/workinghours", new StringContent(body, System.Text.Encoding.UTF8, "application/json"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await GetHoursAsync(owner))[(int)DayOfWeek.Tuesday].IsActive.Should().BeTrue("a rejected request must not replace the schedule");
    }
}
