using System.Net;
using System.Net.Http.Json;
using BookingHubAPI.Application.DTOs;
using BookingHubAPI.IntegrationTests.Support;
using FluentAssertions;
using Xunit;

namespace BookingHubAPI.IntegrationTests;

/// <summary>
/// Characterization tests for GET /api/availability/{serviceId}. They pin the slots computed from
/// the company's working hours and reservations so the computation can move out of the controller
/// without changing it. Each test registers its own owner, so tests never interfere with each other.
/// </summary>
public class AvailabilityControllerTests : IClassFixture<BookingApiFactory>
{
    // A Monday, far enough in the future that reservations on it are never "in the past".
    private static readonly DateTime Monday = new(2030, 1, 7);
    private static readonly DateTime Tuesday = new(2030, 1, 8);

    private readonly BookingApiFactory _factory;

    public AvailabilityControllerTests(BookingApiFactory factory)
    {
        _factory = factory;
    }

    // ---------- helpers ----------

    private static async Task<List<AvailableSlotResponse>> SlotsAsync(TestUser user, ServiceResponse service, DateTime date)
    {
        var response = await user.Client.GetAsync($"/api/availability/{service.Id}?date={date:yyyy-MM-dd}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<List<AvailableSlotResponse>>())!;
    }

    private static IEnumerable<TimeSpan> StartHours(IEnumerable<AvailableSlotResponse> slots) =>
        slots.Select(s => s.StartTime.TimeOfDay);

    private static DateTimeOffset Utc(DateTime wallClock) => new(DateTime.SpecifyKind(wallClock, DateTimeKind.Utc));

    private static TimeSpan H(double hours) => TimeSpan.FromHours(hours);

    private static Task<HttpResponseMessage> BookAsync(TestUser customer, ServiceResponse service, DateTime start) =>
        customer.Client.PostAsJsonAsync(
            "/api/reservations",
            new ReservationRequest(service.Id, DateTime.SpecifyKind(start, DateTimeKind.Utc), null));

    // ---------- authentication and lookup ----------

    [Fact]
    public async Task Availability_WithoutToken_ShouldReturnUnauthorized()
    {
        var response = await _factory.CreateClient().GetAsync($"/api/availability/{Guid.NewGuid()}?date=2030-01-07");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Availability_ForInactiveService_ShouldReturnNotFound()
    {
        var owner = await TestApi.RegisterOwnerAsync(_factory);
        var service = await TestApi.CreateServiceAsync(owner);
        await TestApi.SetWorkingHoursAsync(owner, TestApi.Hours(DayOfWeek.Monday, 9, 12));
        (await owner.Client.DeleteAsync($"/api/services/{service.Id}")).StatusCode.Should().Be(HttpStatusCode.NoContent);
        var customer = await TestApi.RegisterCustomerAsync(_factory);

        var response = await customer.Client.GetAsync($"/api/availability/{service.Id}?date=2030-01-07");

        await response.ShouldBeProblemAsync(HttpStatusCode.NotFound, "Service not found or inactive");
    }

    [Fact]
    public async Task Availability_ForInactiveCompany_ShouldReturnNotFound()
    {
        var owner = await TestApi.RegisterOwnerAsync(_factory);
        var service = await TestApi.CreateServiceAsync(owner);
        await TestApi.SetWorkingHoursAsync(owner, TestApi.Hours(DayOfWeek.Monday, 9, 12));
        await TestApi.DeactivateCompanyAsync(_factory, service.CompanyId);
        var customer = await TestApi.RegisterCustomerAsync(_factory);

        var response = await customer.Client.GetAsync($"/api/availability/{service.Id}?date=2030-01-07");

        await response.ShouldBeProblemAsync(HttpStatusCode.NotFound, "Company not found or inactive");
    }

    // ---------- slots from working hours ----------

    [Fact]
    public async Task Availability_ShouldListConsecutiveSlotsInsideTheWorkingHours()
    {
        var owner = await TestApi.RegisterOwnerAsync(_factory);
        var service = await TestApi.CreateServiceAsync(owner, durationMinutes: 60);
        await TestApi.SetWorkingHoursAsync(owner, TestApi.Hours(DayOfWeek.Monday, 9, 12));
        var customer = await TestApi.RegisterCustomerAsync(_factory);

        var slots = await SlotsAsync(customer, service, Monday);

        slots.Select(s => (s.StartTime, s.EndTime, s.IsAvailable)).Should().Equal(
            (Utc(Monday.AddHours(9)), Utc(Monday.AddHours(10)), true),
            (Utc(Monday.AddHours(10)), Utc(Monday.AddHours(11)), true),
            (Utc(Monday.AddHours(11)), Utc(Monday.AddHours(12)), true));
    }

    [Fact]
    public async Task Availability_WhenDurationDoesNotDivideTheWindow_ShouldDropThePartialLastSlot()
    {
        var owner = await TestApi.RegisterOwnerAsync(_factory);
        var service = await TestApi.CreateServiceAsync(owner, durationMinutes: 45);
        await TestApi.SetWorkingHoursAsync(owner, TestApi.Hours(DayOfWeek.Monday, 9, 11));
        var customer = await TestApi.RegisterCustomerAsync(_factory);

        var slots = await SlotsAsync(customer, service, Monday);

        // 09:00-09:45 and 09:45-10:30 fit; 10:30-11:15 would end after closing.
        StartHours(slots).Should().Equal(H(9), H(9.75));
    }

    [Fact]
    public async Task Availability_WhenServiceIsLongerThanTheWindow_ShouldReturnNoSlots()
    {
        var owner = await TestApi.RegisterOwnerAsync(_factory);
        var service = await TestApi.CreateServiceAsync(owner, durationMinutes: 180);
        await TestApi.SetWorkingHoursAsync(owner, TestApi.Hours(DayOfWeek.Monday, 9, 11));
        var customer = await TestApi.RegisterCustomerAsync(_factory);

        (await SlotsAsync(customer, service, Monday)).Should().BeEmpty();
    }

    [Fact]
    public async Task Availability_ShouldUseTheScheduleOfTheRequestedWeekday()
    {
        var owner = await TestApi.RegisterOwnerAsync(_factory);
        var service = await TestApi.CreateServiceAsync(owner, durationMinutes: 60);
        await TestApi.SetWorkingHoursAsync(
            owner, TestApi.Hours(DayOfWeek.Monday, 9, 10), TestApi.Hours(DayOfWeek.Tuesday, 14, 16));
        var customer = await TestApi.RegisterCustomerAsync(_factory);

        StartHours(await SlotsAsync(customer, service, Monday)).Should().Equal(H(9));
        StartHours(await SlotsAsync(customer, service, Tuesday)).Should().Equal(H(14), H(15));
    }

    [Fact]
    public async Task Availability_OnAnInactiveDay_ShouldReturnNoSlots()
    {
        var owner = await TestApi.RegisterOwnerAsync(_factory);
        var service = await TestApi.CreateServiceAsync(owner);
        await TestApi.SetWorkingHoursAsync(owner, TestApi.Hours(DayOfWeek.Monday, 9, 12, isActive: false));
        var customer = await TestApi.RegisterCustomerAsync(_factory);

        (await SlotsAsync(customer, service, Monday)).Should().BeEmpty();
    }

    [Fact]
    public async Task Availability_WhenNoScheduleIsConfigured_ShouldReturnNoSlots()
    {
        var owner = await TestApi.RegisterOwnerAsync(_factory);
        var service = await TestApi.CreateServiceAsync(owner, openAllWeek: false);
        var customer = await TestApi.RegisterCustomerAsync(_factory);

        (await SlotsAsync(customer, service, Monday)).Should().BeEmpty();
    }

    [Fact]
    public async Task Availability_ShouldIgnoreTheTimeOfDayOfTheRequestedDate()
    {
        var owner = await TestApi.RegisterOwnerAsync(_factory);
        var service = await TestApi.CreateServiceAsync(owner, durationMinutes: 60);
        await TestApi.SetWorkingHoursAsync(owner, TestApi.Hours(DayOfWeek.Monday, 9, 11));
        var customer = await TestApi.RegisterCustomerAsync(_factory);

        var response = await customer.Client.GetAsync($"/api/availability/{service.Id}?date=2030-01-07T23:30:00");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var slots = (await response.Content.ReadFromJsonAsync<List<AvailableSlotResponse>>())!;
        slots.Select(s => s.StartTime).Should().Equal(Utc(Monday.AddHours(9)), Utc(Monday.AddHours(10)));
    }

    // ---------- reservations ----------

    [Fact]
    public async Task Availability_ShouldHideSlotsBlockedByAReservation()
    {
        var owner = await TestApi.RegisterOwnerAsync(_factory);
        var service = await TestApi.CreateServiceAsync(owner, durationMinutes: 60);
        await TestApi.SetWorkingHoursAsync(owner, TestApi.Hours(DayOfWeek.Monday, 9, 12));
        var customer = await TestApi.RegisterCustomerAsync(_factory);
        (await BookAsync(customer, service, Monday.AddHours(10))).StatusCode.Should().Be(HttpStatusCode.Created);

        var slots = await SlotsAsync(customer, service, Monday);

        StartHours(slots).Should().Equal(H(9), H(11));
        slots.Should().OnlyContain(s => s.IsAvailable);
    }

    [Fact]
    public async Task Availability_ShouldOfferASlotAgainOnceItsReservationIsCancelled()
    {
        var owner = await TestApi.RegisterOwnerAsync(_factory);
        var service = await TestApi.CreateServiceAsync(owner, durationMinutes: 60);
        await TestApi.SetWorkingHoursAsync(owner, TestApi.Hours(DayOfWeek.Monday, 9, 12));
        var customer = await TestApi.RegisterCustomerAsync(_factory);
        var booked = await BookAsync(customer, service, Monday.AddHours(10));
        var reservation = (await booked.Content.ReadFromJsonAsync<ReservationResponse>())!;
        (await customer.Client.PutAsync($"/api/reservations/{reservation.Id}/cancel", null))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        StartHours(await SlotsAsync(customer, service, Monday)).Should().Equal(H(9), H(10), H(11));
    }

    [Fact]
    public async Task Availability_ShouldOnlyBlockSlotsOfTheSameService()
    {
        var owner = await TestApi.RegisterOwnerAsync(_factory);
        var service = await TestApi.CreateServiceAsync(owner, durationMinutes: 60);
        var otherService = await TestApi.CreateServiceAsync(owner, durationMinutes: 60);
        await TestApi.SetWorkingHoursAsync(owner, TestApi.Hours(DayOfWeek.Monday, 9, 11));
        var customer = await TestApi.RegisterCustomerAsync(_factory);
        (await BookAsync(customer, otherService, Monday.AddHours(9))).StatusCode.Should().Be(HttpStatusCode.Created);

        StartHours(await SlotsAsync(customer, service, Monday)).Should().Equal(H(9), H(10));
        StartHours(await SlotsAsync(customer, otherService, Monday)).Should().Equal(H(10));
    }

    [Fact]
    public async Task Availability_ShouldHideEverySlotThatOverlapsAReservation()
    {
        var owner = await TestApi.RegisterOwnerAsync(_factory);
        var service = await TestApi.CreateServiceAsync(owner, durationMinutes: 60);
        await TestApi.SetWorkingHoursAsync(owner, TestApi.Hours(DayOfWeek.Monday, 9, 12));
        var customer = await TestApi.RegisterCustomerAsync(_factory);
        // 09:30-10:30 overlaps both the 09:00 and the 10:00 slot.
        (await BookAsync(customer, service, Monday.AddHours(9.5))).StatusCode.Should().Be(HttpStatusCode.Created);

        StartHours(await SlotsAsync(customer, service, Monday)).Should().Equal(H(11));
    }
}
