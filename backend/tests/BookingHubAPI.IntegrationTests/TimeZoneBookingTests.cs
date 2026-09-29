using System.Net;
using System.Net.Http.Json;
using System.Text;
using BookingHubAPI.Application.DTOs;
using BookingHubAPI.IntegrationTests.Support;
using FluentAssertions;
using Xunit;

namespace BookingHubAPI.IntegrationTests;

/// <summary>
/// Time zone contract of availability and booking. Working hours are wall-clock times in the company's
/// time zone; reservations are UTC instants; the API returns slot and reservation times in the company's
/// local time with the offset in force at that moment, and never offers a slot that already started.
/// </summary>
public class TimeZoneBookingTests : IClassFixture<BookingApiFactory>
{
    private const string BuenosAires = "America/Argentina/Buenos_Aires"; // UTC-03:00 all year
    private const string Madrid = "Europe/Madrid";                       // +01:00 winter, +02:00 summer
    private const string OutsideHours = "The selected time is outside the company's working hours";

    private readonly BookingApiFactory _factory;

    public TimeZoneBookingTests(BookingApiFactory factory)
    {
        _factory = factory;
        // A fixture is shared by the tests of this class, which run one at a time: start each on real time.
        _factory.Clock.Unpin();
    }

    // ---------- helpers ----------

    private static DateTimeOffset At(int year, int month, int day, int hour, int offsetHours) =>
        new(year, month, day, hour, 0, 0, TimeSpan.FromHours(offsetHours));

    private static async Task<ServiceResponse> OpenAsync(
        TestUser owner, string timeZone, DayOfWeek day, int from, int to, params DayOfWeek[] alsoDays)
    {
        var service = await TestApi.CreateServiceAsync(owner, durationMinutes: 60, openAllWeek: false);
        var days = new[] { day }.Concat(alsoDays).Select(d => TestApi.Hours(d, from, to)).ToArray();
        await TestApi.SetWorkingHoursAsync(owner, days);
        var update = await owner.Client.PutAsJsonAsync("/api/companies/me", new CompanyUpdateRequest(null, null, timeZone));
        update.StatusCode.Should().Be(HttpStatusCode.OK);
        return service;
    }

    private static async Task<List<AvailableSlotResponse>> SlotsAsync(TestUser user, ServiceResponse service, string date)
    {
        var response = await user.Client.GetAsync($"/api/availability/{service.Id}?date={date}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<List<AvailableSlotResponse>>())!;
    }

    private static Task<HttpResponseMessage> BookAsync(TestUser customer, ServiceResponse service, DateTimeOffset start) =>
        customer.Client.PostAsJsonAsync("/api/reservations", new ReservationRequest(service.Id, start, null));

    private static Task<HttpResponseMessage> BookRawAsync(TestUser customer, ServiceResponse service, string startTimeJson) =>
        customer.Client.PostAsync(
            "/api/reservations",
            new StringContent(
                "{\"serviceId\":\"" + service.Id + "\",\"startTime\":" + startTimeJson + "}",
                Encoding.UTF8,
                "application/json"));

    private static async Task<PagedResult<ReservationResponse>> ListAsync(TestUser user) =>
        (await (await user.Client.GetAsync("/api/reservations")).Content
            .ReadFromJsonAsync<PagedResult<ReservationResponse>>())!;

    // ---------- availability: local time with offset ----------

    [Fact]
    public async Task Availability_ForANonUtcCompany_ReadsWorkingHoursInItsTimeZone()
    {
        var owner = await TestApi.RegisterOwnerAsync(_factory);
        var service = await OpenAsync(owner, BuenosAires, DayOfWeek.Monday, 9, 11);
        var customer = await TestApi.RegisterCustomerAsync(_factory);

        var slots = await SlotsAsync(customer, service, "2030-01-07");

        slots.Select(s => s.StartTime).Should().Equal(At(2030, 1, 7, 9, -3), At(2030, 1, 7, 10, -3));
        slots.Select(s => s.StartTime.Offset).Should().OnlyContain(o => o == TimeSpan.FromHours(-3));
        // The same slots as absolute instants: 09:00 in Buenos Aires is 12:00Z.
        slots.Select(s => s.StartTime.UtcDateTime.Hour).Should().Equal(12, 13);
        slots.Select(s => s.EndTime).Should().Equal(At(2030, 1, 7, 10, -3), At(2030, 1, 7, 11, -3));
    }

    [Fact]
    public async Task Availability_ForAUtcCompany_ReturnsUtcSlots()
    {
        var owner = await TestApi.RegisterOwnerAsync(_factory);
        var service = await OpenAsync(owner, "UTC", DayOfWeek.Monday, 9, 10);
        var customer = await TestApi.RegisterCustomerAsync(_factory);

        var slots = await SlotsAsync(customer, service, "2030-01-07");

        slots.Should().ContainSingle().Which.StartTime.Should().Be(At(2030, 1, 7, 9, 0));
        slots[0].StartTime.Offset.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public async Task Availability_ShouldSerializeSlotsAsIso8601WithTheCompanyOffset()
    {
        var owner = await TestApi.RegisterOwnerAsync(_factory);
        var service = await OpenAsync(owner, BuenosAires, DayOfWeek.Monday, 9, 10);
        var customer = await TestApi.RegisterCustomerAsync(_factory);

        var json = await customer.Client.GetStringAsync($"/api/availability/{service.Id}?date=2030-01-07");

        json.Should().Contain("\"startTime\":\"2030-01-07T09:00:00-03:00\"");
        json.Should().Contain("\"endTime\":\"2030-01-07T10:00:00-03:00\"");
    }

    [Fact]
    public async Task Availability_AcrossADaylightSavingChange_UsesTheOffsetOfEachDay()
    {
        // Madrid springs forward on Sunday 2030-03-31 (02:00 -> 03:00).
        var owner = await TestApi.RegisterOwnerAsync(_factory);
        var service = await OpenAsync(owner, Madrid, DayOfWeek.Saturday, 9, 10, DayOfWeek.Sunday);
        var customer = await TestApi.RegisterCustomerAsync(_factory);

        var saturday = await SlotsAsync(customer, service, "2030-03-30");
        var sunday = await SlotsAsync(customer, service, "2030-03-31");

        saturday.Should().ContainSingle().Which.StartTime.Should().Be(At(2030, 3, 30, 9, 1));
        saturday[0].StartTime.Offset.Should().Be(TimeSpan.FromHours(1));
        sunday.Should().ContainSingle().Which.StartTime.Should().Be(At(2030, 3, 31, 9, 2));
        sunday[0].StartTime.Offset.Should().Be(TimeSpan.FromHours(2));
        // Both read 09:00 on the wall clock, but only 23 real hours apart.
        (sunday[0].StartTime - saturday[0].StartTime).Should().Be(TimeSpan.FromHours(23));
    }

    // ---------- availability: past slots ----------

    [Fact]
    public async Task Availability_ForADayInThePast_ShouldReturnNoSlots()
    {
        var owner = await TestApi.RegisterOwnerAsync(_factory);
        var service = await OpenAsync(owner, "UTC", DayOfWeek.Monday, 9, 10);
        new DateTime(2020, 1, 6).DayOfWeek.Should().Be(DayOfWeek.Monday);
        var customer = await TestApi.RegisterCustomerAsync(_factory);

        (await SlotsAsync(customer, service, "2020-01-06")).Should().BeEmpty();
    }

    [Fact]
    public async Task Availability_ShouldHideOnlyTheSlotsThatAlreadyStarted()
    {
        var owner = await TestApi.RegisterOwnerAsync(_factory);
        var service = await OpenAsync(owner, "UTC", DayOfWeek.Monday, 9, 12);
        var customer = await TestApi.RegisterCustomerAsync(_factory);
        _factory.Clock.Pin(At(2030, 1, 7, 10, 0).AddMinutes(30));

        var slots = await SlotsAsync(customer, service, "2030-01-07");

        slots.Select(s => s.StartTime).Should().Equal(At(2030, 1, 7, 11, 0));
    }

    [Fact]
    public async Task Availability_JudgesThePastInAbsoluteTimeForANonUtcCompany()
    {
        var owner = await TestApi.RegisterOwnerAsync(_factory);
        var service = await OpenAsync(owner, BuenosAires, DayOfWeek.Monday, 9, 12);
        var customer = await TestApi.RegisterCustomerAsync(_factory);
        // 12:30Z is 09:30 in Buenos Aires: the 09:00 local slot is over, 10:00 and 11:00 are still offered.
        _factory.Clock.Pin(new DateTimeOffset(2030, 1, 7, 12, 30, 0, TimeSpan.Zero));

        var slots = await SlotsAsync(customer, service, "2030-01-07");

        slots.Select(s => s.StartTime).Should().Equal(At(2030, 1, 7, 10, -3), At(2030, 1, 7, 11, -3));
    }

    // ---------- booking ----------

    [Fact]
    public async Task Create_ForANonUtcCompany_AcceptsAStartInsideTheLocalWindow()
    {
        var owner = await TestApi.RegisterOwnerAsync(_factory);
        var service = await OpenAsync(owner, BuenosAires, DayOfWeek.Monday, 9, 11);
        var customer = await TestApi.RegisterCustomerAsync(_factory);

        var response = await BookAsync(customer, service, At(2030, 1, 7, 9, -3));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = (await response.Content.ReadFromJsonAsync<ReservationResponse>())!;
        body.StartTime.Should().Be(At(2030, 1, 7, 9, -3));
        body.StartTime.Offset.Should().Be(TimeSpan.FromHours(-3));
        body.EndTime.Should().Be(At(2030, 1, 7, 10, -3));
    }

    [Fact]
    public async Task Create_TheSameInstantInAnotherOffset_ConflictsWithTheExistingReservation()
    {
        var owner = await TestApi.RegisterOwnerAsync(_factory);
        var service = await OpenAsync(owner, BuenosAires, DayOfWeek.Monday, 9, 11);
        var customer = await TestApi.RegisterCustomerAsync(_factory);
        (await BookAsync(customer, service, At(2030, 1, 7, 9, -3))).StatusCode.Should().Be(HttpStatusCode.Created);

        var sameInstantAsUtc = await BookAsync(customer, service, At(2030, 1, 7, 12, 0));

        await sameInstantAsUtc.ShouldBeProblemAsync(HttpStatusCode.Conflict, "Time slot is not available");
        (await SlotsAsync(customer, service, "2030-01-07")).Select(s => s.StartTime)
            .Should().Equal(At(2030, 1, 7, 10, -3));
    }

    [Fact]
    public async Task Create_AtTheUtcHourOfTheWindow_IsRejectedForANonUtcCompany()
    {
        var owner = await TestApi.RegisterOwnerAsync(_factory);
        var service = await OpenAsync(owner, BuenosAires, DayOfWeek.Monday, 9, 11);
        var customer = await TestApi.RegisterCustomerAsync(_factory);

        // 09:00Z is 06:00 in Buenos Aires, before opening.
        var response = await BookAsync(customer, service, At(2030, 1, 7, 9, 0));

        await response.ShouldBeProblemAsync(HttpStatusCode.BadRequest, OutsideHours);
    }

    [Fact]
    public async Task Create_AfterAUtcDayRollover_UsesTheLocalDayOfTheCompany()
    {
        // 2030-01-08T01:00Z is still Monday 22:00 in Buenos Aires, which is open on Monday until 23:00.
        var owner = await TestApi.RegisterOwnerAsync(_factory);
        var service = await OpenAsync(owner, BuenosAires, DayOfWeek.Monday, 9, 23);
        var customer = await TestApi.RegisterCustomerAsync(_factory);

        var response = await BookAsync(customer, service, At(2030, 1, 8, 1, 0));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Create_AcrossADaylightSavingChange_UsesTheOffsetInForceThatDay()
    {
        // Sunday 2030-03-31 in Madrid is +02:00 all day after the 02:00 change: 09:00 local is 07:00Z.
        var owner = await TestApi.RegisterOwnerAsync(_factory);
        var service = await OpenAsync(owner, Madrid, DayOfWeek.Sunday, 9, 11);
        var customer = await TestApi.RegisterCustomerAsync(_factory);

        var before = await BookAsync(customer, service, At(2030, 3, 31, 6, 0)); // 08:00 local: before opening
        var onTime = await BookAsync(customer, service, At(2030, 3, 31, 7, 0));

        await before.ShouldBeProblemAsync(HttpStatusCode.BadRequest, OutsideHours);
        onTime.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = (await onTime.Content.ReadFromJsonAsync<ReservationResponse>())!;
        body.StartTime.Should().Be(At(2030, 3, 31, 9, 2));
        body.StartTime.Offset.Should().Be(TimeSpan.FromHours(2));
    }

    [Fact]
    public async Task Create_InThePastInAbsoluteTime_IsRejected()
    {
        var owner = await TestApi.RegisterOwnerAsync(_factory);
        var service = await OpenAsync(owner, BuenosAires, DayOfWeek.Monday, 9, 12);
        var customer = await TestApi.RegisterCustomerAsync(_factory);
        // 12:30Z is 09:30 in Buenos Aires, so the 09:00 local (12:00Z) start has already passed.
        _factory.Clock.Pin(new DateTimeOffset(2030, 1, 7, 12, 30, 0, TimeSpan.Zero));

        var response = await BookAsync(customer, service, At(2030, 1, 7, 9, -3));

        await response.ShouldBeProblemAsync(HttpStatusCode.BadRequest, "Cannot book in the past");
    }

    [Fact]
    public async Task ReservationLists_ReturnTimesInTheCompanyTimeZone()
    {
        var owner = await TestApi.RegisterOwnerAsync(_factory);
        var service = await OpenAsync(owner, BuenosAires, DayOfWeek.Monday, 9, 11);
        var customer = await TestApi.RegisterCustomerAsync(_factory);
        (await BookAsync(customer, service, At(2030, 1, 7, 12, 0))).StatusCode.Should().Be(HttpStatusCode.Created);

        foreach (var user in new[] { customer, owner })
        {
            var reservation = (await ListAsync(user)).Items.Should().ContainSingle().Subject;
            reservation.StartTime.Offset.Should().Be(TimeSpan.FromHours(-3));
            reservation.StartTime.Hour.Should().Be(9);
        }
    }

    // ---------- booking: offset-less input ----------

    [Theory]
    [InlineData("\"2030-01-07T09:00:00\"")]      // no offset
    [InlineData("\"2030-01-07\"")]               // date only
    [InlineData("\"2030-01-07T09:00:00+0300\"")] // offset without colon
    [InlineData("\"not a date\"")]
    [InlineData("null")]
    public async Task Create_WithoutAUtcOffset_ShouldReturnBadRequestAndCreateNothing(string startTimeJson)
    {
        var owner = await TestApi.RegisterOwnerAsync(_factory);
        var service = await OpenAsync(owner, BuenosAires, DayOfWeek.Monday, 9, 11);
        var customer = await TestApi.RegisterCustomerAsync(_factory);

        var response = await BookRawAsync(customer, service, startTimeJson);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await ListAsync(customer)).Items.Should().BeEmpty();
    }

    [Theory]
    [InlineData("\"2030-01-07T09:00:00-03:00\"")]
    [InlineData("\"2030-01-07T12:00:00Z\"")]
    [InlineData("\"2030-01-07T12:00:00.000+00:00\"")]
    public async Task Create_WithAnyIso8601Offset_ShouldBeAccepted(string startTimeJson)
    {
        var owner = await TestApi.RegisterOwnerAsync(_factory);
        var service = await OpenAsync(owner, BuenosAires, DayOfWeek.Monday, 9, 11);
        var customer = await TestApi.RegisterCustomerAsync(_factory);

        var response = await BookRawAsync(customer, service, startTimeJson);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }
}
