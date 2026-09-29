using System.Net;
using System.Net.Http.Json;
using BookingHubAPI.Application.DTOs;
using BookingHubAPI.IntegrationTests.Support;
using FluentAssertions;
using Xunit;

namespace BookingHubAPI.IntegrationTests;

/// <summary>
/// Characterization tests for /api/reservations. They pin the observable HTTP behavior
/// (status codes and body shapes) so the controller can be refactored without changing it.
/// Each test registers its own users/services, so tests never interfere with each other.
/// </summary>
public class ReservationsControllerTests : IClassFixture<BookingApiFactory>
{
    private readonly BookingApiFactory _factory;

    public ReservationsControllerTests(BookingApiFactory factory)
    {
        _factory = factory;
    }

    // ---------- helpers ----------

    /// <summary>Books a start given as a wall-clock value read as UTC (the default test company is in UTC).</summary>
    private static Task<HttpResponseMessage> BookAsync(
        TestUser customer, ServiceResponse service, DateTime start, string? notes = null) =>
        BookAsync(customer, service, new DateTimeOffset(DateTime.SpecifyKind(start, DateTimeKind.Utc)), notes);

    private static Task<HttpResponseMessage> BookAsync(
        TestUser customer, ServiceResponse service, DateTimeOffset start, string? notes = null) =>
        customer.Client.PostAsJsonAsync("/api/reservations", new ReservationRequest(service.Id, start, notes));

    private static async Task<ReservationResponse> BookOkAsync(
        TestUser customer, ServiceResponse service, DateTime start, string? notes = null)
    {
        var response = await BookAsync(customer, service, start, notes);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<ReservationResponse>())!;
    }

    private static async Task SetDurationAsync(TestUser owner, ServiceResponse service, int durationMinutes)
    {
        var response = await owner.Client.PutAsJsonAsync(
            $"/api/services/{service.Id}", new ServiceUpdateRequest(null, null, durationMinutes, null, null));
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private static async Task AssertErrorAsync(HttpResponseMessage response, HttpStatusCode status, string message)
    {
        await response.ShouldBeProblemAsync(status, message);
    }

    private static async Task<PagedResult<ReservationResponse>> ListAsync(TestUser user, string query = "")
    {
        var response = await user.Client.GetAsync("/api/reservations" + query);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        return (await response.Content.ReadFromJsonAsync<PagedResult<ReservationResponse>>())!;
    }

    // ---------- authentication ----------

    [Theory]
    [InlineData("GET", "/api/reservations")]
    [InlineData("POST", "/api/reservations")]
    [InlineData("PUT", "/api/reservations/00000000-0000-0000-0000-000000000001/confirm")]
    [InlineData("PUT", "/api/reservations/00000000-0000-0000-0000-000000000001/cancel")]
    public async Task AnyEndpoint_WithoutToken_ShouldReturnUnauthorized(string method, string url)
    {
        var client = _factory.CreateClient();

        var response = await client.SendAsync(new HttpRequestMessage(new HttpMethod(method), url));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ---------- GET /api/reservations ----------

    [Fact]
    public async Task List_ForCustomerWithoutReservations_ShouldReturnEmptyPage()
    {
        var customer = await TestApi.RegisterCustomerAsync(_factory);

        var page = await ListAsync(customer);

        page.Items.Should().BeEmpty();
        page.TotalCount.Should().Be(0);
        page.Page.Should().Be(1);
        page.PageSize.Should().Be(10);
        page.TotalPages.Should().Be(0);
    }

    [Fact]
    public async Task List_ForCustomer_ShouldReturnOnlyOwnReservations()
    {
        var owner = await TestApi.RegisterOwnerAsync(_factory);
        var service = await TestApi.CreateServiceAsync(owner);
        var alice = await TestApi.RegisterCustomerAsync(_factory);
        var bob = await TestApi.RegisterCustomerAsync(_factory);
        var aliceReservation = await BookOkAsync(alice, service, TestApi.FutureSlot(hour: 9));
        await BookOkAsync(bob, service, TestApi.FutureSlot(hour: 12));

        var page = await ListAsync(alice);

        page.TotalCount.Should().Be(1);
        var item = page.Items.Should().ContainSingle().Subject;
        item.Id.Should().Be(aliceReservation.Id);
        item.CustomerId.Should().Be(alice.UserId);
        item.CustomerEmail.Should().Be(alice.Email);
        item.ServiceId.Should().Be(service.Id);
        item.ServiceName.Should().Be(service.Name);
        item.ServiceDuration.Should().Be(60);
        item.Status.Should().Be("Pending");
    }

    [Fact]
    public async Task List_ForOwner_ShouldReturnAllReservationsOfOwnCompanyOnly()
    {
        var owner = await TestApi.RegisterOwnerAsync(_factory);
        var otherOwner = await TestApi.RegisterOwnerAsync(_factory);
        var service = await TestApi.CreateServiceAsync(owner);
        var otherService = await TestApi.CreateServiceAsync(otherOwner);
        var alice = await TestApi.RegisterCustomerAsync(_factory);
        var bob = await TestApi.RegisterCustomerAsync(_factory);
        await BookOkAsync(alice, service, TestApi.FutureSlot(hour: 9));
        await BookOkAsync(bob, service, TestApi.FutureSlot(hour: 12));
        await BookOkAsync(bob, otherService, TestApi.FutureSlot(hour: 9));

        var page = await ListAsync(owner);

        page.TotalCount.Should().Be(2);
        page.Items.Select(r => r.CustomerEmail).Should().BeEquivalentTo(new[] { alice.Email, bob.Email });
        page.Items.Should().OnlyContain(r => r.ServiceId == service.Id);
    }

    [Fact]
    public async Task List_ShouldPaginateNewestFirstAndFilterByStatusCaseInsensitively()
    {
        var owner = await TestApi.RegisterOwnerAsync(_factory);
        var service = await TestApi.CreateServiceAsync(owner);
        var customer = await TestApi.RegisterCustomerAsync(_factory);
        var first = await BookOkAsync(customer, service, TestApi.FutureSlot(hour: 9));
        var second = await BookOkAsync(customer, service, TestApi.FutureSlot(hour: 11));
        var third = await BookOkAsync(customer, service, TestApi.FutureSlot(hour: 13));
        (await customer.Client.PutAsync($"/api/reservations/{second.Id}/cancel", null))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        var firstPage = await ListAsync(customer, "?page=1&pageSize=2");
        var secondPage = await ListAsync(customer, "?page=2&pageSize=2");
        var cancelled = await ListAsync(customer, "?status=cancelled");
        var unknownStatus = await ListAsync(customer, "?status=Nonsense");

        firstPage.Items.Select(r => r.Id).Should().Equal(third.Id, second.Id);
        firstPage.TotalCount.Should().Be(3);
        firstPage.TotalPages.Should().Be(2);
        firstPage.Page.Should().Be(1);
        firstPage.PageSize.Should().Be(2);
        secondPage.Items.Select(r => r.Id).Should().Equal(first.Id);
        cancelled.Items.Select(r => r.Id).Should().Equal(second.Id);
        cancelled.TotalCount.Should().Be(1);
        // An unparseable status is silently ignored instead of rejected.
        unknownStatus.TotalCount.Should().Be(3);
    }

    // ---------- POST /api/reservations ----------

    [Fact]
    public async Task Create_WithValidRequest_ShouldReturnCreatedPendingReservation()
    {
        var owner = await TestApi.RegisterOwnerAsync(_factory);
        var service = await TestApi.CreateServiceAsync(owner, durationMinutes: 45);
        var customer = await TestApi.RegisterCustomerAsync(_factory);
        var start = TestApi.FutureSlot();

        var response = await BookAsync(customer, service, start, "Window seat");

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.ToString().Should().EndWith("/api/Reservations");
        var body = (await response.Content.ReadFromJsonAsync<ReservationResponse>())!;
        body.Id.Should().NotBeEmpty();
        body.CustomerId.Should().Be(customer.UserId);
        body.CustomerEmail.Should().Be(customer.Email);
        body.ServiceId.Should().Be(service.Id);
        body.ServiceName.Should().Be(service.Name);
        body.ServiceDuration.Should().Be(45);
        body.StartTime.Should().Be(new DateTimeOffset(start));
        body.EndTime.Should().Be(new DateTimeOffset(start.AddMinutes(45)));
        body.Status.Should().Be("Pending");
        body.Notes.Should().Be("Window seat");
        body.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task Create_ForUnknownService_ShouldReturnBadRequest()
    {
        var customer = await TestApi.RegisterCustomerAsync(_factory);

        var response = await customer.Client.PostAsJsonAsync(
            "/api/reservations", new ReservationRequest(Guid.NewGuid(), TestApi.FutureSlot(), null));

        await AssertErrorAsync(response, HttpStatusCode.BadRequest, "Service not found or inactive");
    }

    [Fact]
    public async Task Create_WithEmptyServiceId_ShouldReturnBadRequestServiceNotFound()
    {
        // [Required] does not reject Guid.Empty (a value type), so it reaches the lookup.
        var customer = await TestApi.RegisterCustomerAsync(_factory);

        var response = await customer.Client.PostAsJsonAsync(
            "/api/reservations", new ReservationRequest(Guid.Empty, TestApi.FutureSlot(), null));

        await AssertErrorAsync(response, HttpStatusCode.BadRequest, "Service not found or inactive");
    }

    [Fact]
    public async Task Create_ForInactiveService_ShouldReturnBadRequest()
    {
        var owner = await TestApi.RegisterOwnerAsync(_factory);
        var service = await TestApi.CreateServiceAsync(owner);
        (await owner.Client.DeleteAsync($"/api/services/{service.Id}")).StatusCode.Should().Be(HttpStatusCode.NoContent);
        var customer = await TestApi.RegisterCustomerAsync(_factory);

        var response = await BookAsync(customer, service, TestApi.FutureSlot());

        await AssertErrorAsync(response, HttpStatusCode.BadRequest, "Service not found or inactive");
    }

    [Fact]
    public async Task Create_ForInactiveCompany_ShouldReturnBadRequest()
    {
        var owner = await TestApi.RegisterOwnerAsync(_factory);
        var service = await TestApi.CreateServiceAsync(owner);
        await TestApi.DeactivateCompanyAsync(_factory, service.CompanyId);
        var customer = await TestApi.RegisterCustomerAsync(_factory);

        var response = await BookAsync(customer, service, TestApi.FutureSlot());

        await AssertErrorAsync(response, HttpStatusCode.BadRequest, "Company not found or inactive");
    }

    [Fact]
    public async Task Create_InThePast_ShouldReturnBadRequest()
    {
        var owner = await TestApi.RegisterOwnerAsync(_factory);
        var service = await TestApi.CreateServiceAsync(owner);
        var customer = await TestApi.RegisterCustomerAsync(_factory);

        var response = await BookAsync(customer, service, DateTime.UtcNow.AddDays(-1));

        await AssertErrorAsync(response, HttpStatusCode.BadRequest, "Cannot book in the past");
    }

    // ---------- POST /api/reservations: working hours ----------

    private const string OutsideHoursMessage = "The selected time is outside the company's working hours";

    [Theory]
    [InlineData(8, 60)]    // before the 09:00 opening
    [InlineData(17, 60)]   // starts at the 17:00 closing
    [InlineData(20, 60)]   // well after closing
    [InlineData(16, 120)]  // starts inside but spans the closing time
    [InlineData(8, 120)]   // starts before opening and ends inside
    public async Task Create_OutsideWorkingHours_ShouldReturnBadRequest(int startHour, int durationMinutes)
    {
        var owner = await TestApi.RegisterOwnerAsync(_factory);
        var service = await TestApi.CreateServiceAsync(owner, durationMinutes);
        var customer = await TestApi.RegisterCustomerAsync(_factory);

        var response = await BookAsync(customer, service, TestApi.FutureSlot(hour: startHour));

        await AssertErrorAsync(response, HttpStatusCode.BadRequest, OutsideHoursMessage);
        (await ListAsync(customer)).TotalCount.Should().Be(0);
    }

    [Theory]
    [InlineData(9, 60)]    // starts exactly at opening
    [InlineData(16, 60)]   // ends exactly at closing
    [InlineData(9, 480)]   // fills the whole day
    public async Task Create_TouchingTheWorkingHoursBoundaries_ShouldSucceed(int startHour, int durationMinutes)
    {
        var owner = await TestApi.RegisterOwnerAsync(_factory);
        var service = await TestApi.CreateServiceAsync(owner, durationMinutes);
        var customer = await TestApi.RegisterCustomerAsync(_factory);

        var response = await BookAsync(customer, service, TestApi.FutureSlot(hour: startHour));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Create_OnAnInactiveDay_ShouldReturnBadRequest()
    {
        var owner = await TestApi.RegisterOwnerAsync(_factory);
        var service = await TestApi.CreateServiceAsync(owner);
        var start = TestApi.FutureSlot();
        await TestApi.SetWorkingHoursAsync(owner, TestApi.Hours(start.DayOfWeek, 9, 17, isActive: false));
        var customer = await TestApi.RegisterCustomerAsync(_factory);

        var response = await BookAsync(customer, service, start);

        await AssertErrorAsync(response, HttpStatusCode.BadRequest, OutsideHoursMessage);
    }

    [Fact]
    public async Task Create_OnADayWithoutAnyHoursConfigured_ShouldReturnBadRequest()
    {
        var owner = await TestApi.RegisterOwnerAsync(_factory);
        var service = await TestApi.CreateServiceAsync(owner, openAllWeek: false);
        var customer = await TestApi.RegisterCustomerAsync(_factory);

        var response = await BookAsync(customer, service, TestApi.FutureSlot());

        await AssertErrorAsync(response, HttpStatusCode.BadRequest, OutsideHoursMessage);
    }

    [Fact]
    public async Task Create_SpanningMidnight_ShouldReturnBadRequestEvenWhenTheDayRunsUntilMidnight()
    {
        var owner = await TestApi.RegisterOwnerAsync(_factory);
        var service = await TestApi.CreateServiceAsync(owner, durationMinutes: 120);
        var start = TestApi.FutureSlot(hour: 23);
        await TestApi.SetWorkingHoursAsync(
            owner, TestApi.Hours(start.DayOfWeek, 0, 24), TestApi.Hours(start.AddDays(1).DayOfWeek, 0, 24));
        var customer = await TestApi.RegisterCustomerAsync(_factory);

        var response = await BookAsync(customer, service, start);

        await AssertErrorAsync(response, HttpStatusCode.BadRequest, OutsideHoursMessage);
    }

    [Fact]
    public async Task Create_ShouldHonourTheScheduleOfTheDayTheBookingStartsOn()
    {
        var owner = await TestApi.RegisterOwnerAsync(_factory);
        var service = await TestApi.CreateServiceAsync(owner);
        var monday = TestApi.FutureSlot();
        while (monday.DayOfWeek != DayOfWeek.Monday)
        {
            monday = monday.AddDays(1);
        }

        await TestApi.SetWorkingHoursAsync(owner, TestApi.Hours(DayOfWeek.Monday, 14, 18));
        var customer = await TestApi.RegisterCustomerAsync(_factory);

        var morning = await BookAsync(customer, service, monday.Date.AddHours(10));
        var afternoon = await BookAsync(customer, service, monday.Date.AddHours(15));

        await AssertErrorAsync(morning, HttpStatusCode.BadRequest, OutsideHoursMessage);
        afternoon.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Create_ForEverySlotOfferedByAvailability_ShouldSucceed()
    {
        // Booking and availability share one rule: whatever the endpoint offers must be bookable.
        var owner = await TestApi.RegisterOwnerAsync(_factory);
        var service = await TestApi.CreateServiceAsync(owner, durationMinutes: 45);
        var customer = await TestApi.RegisterCustomerAsync(_factory);
        var day = TestApi.FutureSlot().Date;

        var offered = await customer.Client.GetFromJsonAsync<List<AvailableSlotResponse>>(
            $"/api/availability/{service.Id}?date={day:yyyy-MM-dd}");

        offered.Should().HaveCount(10);
        foreach (var slot in offered!)
        {
            (await BookAsync(customer, service, slot.StartTime)).StatusCode.Should().Be(HttpStatusCode.Created);
        }
    }

    [Fact]
    public async Task Create_WithNotesOverLimit_ShouldReturnValidationProblem()
    {
        var owner = await TestApi.RegisterOwnerAsync(_factory);
        var service = await TestApi.CreateServiceAsync(owner);
        var customer = await TestApi.RegisterCustomerAsync(_factory);

        var response = await BookAsync(customer, service, TestApi.FutureSlot(), new string('x', 501));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("Notes");
    }

    [Fact]
    public async Task Create_WithMalformedBody_ShouldReturnBadRequest()
    {
        var customer = await TestApi.RegisterCustomerAsync(_factory);

        var response = await customer.Client.PostAsync(
            "/api/reservations", new StringContent("{ not json", System.Text.Encoding.UTF8, "application/json"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Create_WithExactlyOverlappingSlot_ShouldReturnConflict()
    {
        var owner = await TestApi.RegisterOwnerAsync(_factory);
        var service = await TestApi.CreateServiceAsync(owner);
        var alice = await TestApi.RegisterCustomerAsync(_factory);
        var bob = await TestApi.RegisterCustomerAsync(_factory);
        var start = TestApi.FutureSlot();
        await BookOkAsync(alice, service, start);

        var response = await BookAsync(bob, service, start);

        await AssertErrorAsync(response, HttpStatusCode.Conflict, "Time slot is not available");
    }

    [Theory]
    [InlineData(30)]   // starts inside the existing 60 minute booking
    [InlineData(-30)]  // ends inside the existing booking
    [InlineData(15)]   // starts 15 minutes in and ends 75 minutes in: overlaps the existing booking at its tail
    public async Task Create_WithPartiallyOverlappingSlot_ShouldReturnConflict(int offsetMinutes)
    {
        var owner = await TestApi.RegisterOwnerAsync(_factory);
        var service = await TestApi.CreateServiceAsync(owner, durationMinutes: 60);
        var alice = await TestApi.RegisterCustomerAsync(_factory);
        var bob = await TestApi.RegisterCustomerAsync(_factory);
        var start = TestApi.FutureSlot();
        await BookOkAsync(alice, service, start);

        var response = await BookAsync(bob, service, start.AddMinutes(offsetMinutes));

        await AssertErrorAsync(response, HttpStatusCode.Conflict, "Time slot is not available");
    }

    [Fact]
    public async Task Create_StrictlyInsideExistingBooking_ShouldReturnConflict()
    {
        var owner = await TestApi.RegisterOwnerAsync(_factory);
        var service = await TestApi.CreateServiceAsync(owner, durationMinutes: 60);
        var alice = await TestApi.RegisterCustomerAsync(_factory);
        var bob = await TestApi.RegisterCustomerAsync(_factory);
        var start = TestApi.FutureSlot();
        await BookOkAsync(alice, service, start);
        // The stored booking keeps its own end time; shrinking the service makes the next booking shorter.
        await SetDurationAsync(owner, service, 15);

        // [start+15, start+30] lies strictly inside the existing [start, start+60].
        var response = await BookAsync(bob, service, start.AddMinutes(15));

        await AssertErrorAsync(response, HttpStatusCode.Conflict, "Time slot is not available");
    }

    [Fact]
    public async Task Create_FullyEnclosingExistingBooking_ShouldReturnConflict()
    {
        var owner = await TestApi.RegisterOwnerAsync(_factory);
        var service = await TestApi.CreateServiceAsync(owner, durationMinutes: 15);
        var alice = await TestApi.RegisterCustomerAsync(_factory);
        var bob = await TestApi.RegisterCustomerAsync(_factory);
        var start = TestApi.FutureSlot();
        await BookOkAsync(alice, service, start.AddMinutes(15));
        await SetDurationAsync(owner, service, 60);

        // [start, start+60] fully encloses the existing [start+15, start+30].
        var response = await BookAsync(bob, service, start);

        await AssertErrorAsync(response, HttpStatusCode.Conflict, "Time slot is not available");
    }

    [Fact]
    public async Task Create_WithAdjacentSlots_ShouldSucceedOnBothSides()
    {
        var owner = await TestApi.RegisterOwnerAsync(_factory);
        var service = await TestApi.CreateServiceAsync(owner, durationMinutes: 60);
        var alice = await TestApi.RegisterCustomerAsync(_factory);
        var bob = await TestApi.RegisterCustomerAsync(_factory);
        var carol = await TestApi.RegisterCustomerAsync(_factory);
        var start = TestApi.FutureSlot();
        await BookOkAsync(alice, service, start);

        var after = await BookAsync(bob, service, start.AddMinutes(60));
        var before = await BookAsync(carol, service, start.AddMinutes(-60));

        after.StatusCode.Should().Be(HttpStatusCode.Created);
        before.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Create_ForSlotHeldByCancelledReservation_ShouldSucceed()
    {
        var owner = await TestApi.RegisterOwnerAsync(_factory);
        var service = await TestApi.CreateServiceAsync(owner);
        var alice = await TestApi.RegisterCustomerAsync(_factory);
        var bob = await TestApi.RegisterCustomerAsync(_factory);
        var start = TestApi.FutureSlot();
        var reservation = await BookOkAsync(alice, service, start);
        (await alice.Client.PutAsync($"/api/reservations/{reservation.Id}/cancel", null))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        var response = await BookAsync(bob, service, start);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Create_ForSameSlotOnDifferentService_ShouldSucceed()
    {
        var owner = await TestApi.RegisterOwnerAsync(_factory);
        var service = await TestApi.CreateServiceAsync(owner);
        var otherService = await TestApi.CreateServiceAsync(owner);
        var alice = await TestApi.RegisterCustomerAsync(_factory);
        var bob = await TestApi.RegisterCustomerAsync(_factory);
        var start = TestApi.FutureSlot();
        await BookOkAsync(alice, service, start);

        var response = await BookAsync(bob, otherService, start);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Create_AsOwner_ShouldSucceedOnOwnService()
    {
        // Intended: owners may book on their own services (e.g. entering phone bookings for customers).
        var owner = await TestApi.RegisterOwnerAsync(_factory);
        var service = await TestApi.CreateServiceAsync(owner);

        var response = await BookAsync(owner, service, TestApi.FutureSlot());

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    // ---------- PUT /api/reservations/{id}/confirm ----------

    [Fact]
    public async Task Confirm_ByOwnerOfCompany_ShouldReturnConfirmedReservation()
    {
        var owner = await TestApi.RegisterOwnerAsync(_factory);
        var service = await TestApi.CreateServiceAsync(owner);
        var customer = await TestApi.RegisterCustomerAsync(_factory);
        var reservation = await BookOkAsync(customer, service, TestApi.FutureSlot());

        var response = await owner.Client.PutAsync($"/api/reservations/{reservation.Id}/confirm", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = (await response.Content.ReadFromJsonAsync<ReservationResponse>())!;
        body.Id.Should().Be(reservation.Id);
        body.Status.Should().Be("Confirmed");
        body.CustomerId.Should().Be(customer.UserId);
        body.CustomerEmail.Should().Be(customer.Email);
        body.ServiceName.Should().Be(service.Name);
        body.ServiceDuration.Should().Be(service.DurationMinutes);
        body.StartTime.Should().Be(reservation.StartTime);
        body.EndTime.Should().Be(reservation.EndTime);
    }

    [Fact]
    public async Task Confirm_AsCustomer_ShouldReturnForbidden()
    {
        var owner = await TestApi.RegisterOwnerAsync(_factory);
        var service = await TestApi.CreateServiceAsync(owner);
        var customer = await TestApi.RegisterCustomerAsync(_factory);
        var reservation = await BookOkAsync(customer, service, TestApi.FutureSlot());

        var response = await customer.Client.PutAsync($"/api/reservations/{reservation.Id}/confirm", null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Confirm_ByOwnerOfAnotherCompany_ShouldReturnNotFoundAndLeaveReservationPending()
    {
        var owner = await TestApi.RegisterOwnerAsync(_factory);
        var otherOwner = await TestApi.RegisterOwnerAsync(_factory);
        var service = await TestApi.CreateServiceAsync(owner);
        var customer = await TestApi.RegisterCustomerAsync(_factory);
        var reservation = await BookOkAsync(customer, service, TestApi.FutureSlot());

        var response = await otherOwner.Client.PutAsync($"/api/reservations/{reservation.Id}/confirm", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await ListAsync(customer)).Items.Single().Status.Should().Be("Pending");
    }

    [Fact]
    public async Task Confirm_UnknownReservation_ShouldReturnNotFound()
    {
        var owner = await TestApi.RegisterOwnerAsync(_factory);

        var response = await owner.Client.PutAsync($"/api/reservations/{Guid.NewGuid()}/confirm", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Confirm_WithMalformedId_ShouldReturnBadRequest()
    {
        var owner = await TestApi.RegisterOwnerAsync(_factory);

        var response = await owner.Client.PutAsync("/api/reservations/not-a-guid/confirm", null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Confirm_AlreadyConfirmedReservation_ShouldReturnBadRequest()
    {
        var owner = await TestApi.RegisterOwnerAsync(_factory);
        var service = await TestApi.CreateServiceAsync(owner);
        var customer = await TestApi.RegisterCustomerAsync(_factory);
        var reservation = await BookOkAsync(customer, service, TestApi.FutureSlot());
        await owner.Client.PutAsync($"/api/reservations/{reservation.Id}/confirm", null);

        var response = await owner.Client.PutAsync($"/api/reservations/{reservation.Id}/confirm", null);

        await AssertErrorAsync(response, HttpStatusCode.BadRequest, "Only pending reservations can be confirmed");
    }

    [Fact]
    public async Task Confirm_CancelledReservation_ShouldReturnBadRequest()
    {
        var owner = await TestApi.RegisterOwnerAsync(_factory);
        var service = await TestApi.CreateServiceAsync(owner);
        var customer = await TestApi.RegisterCustomerAsync(_factory);
        var reservation = await BookOkAsync(customer, service, TestApi.FutureSlot());
        await customer.Client.PutAsync($"/api/reservations/{reservation.Id}/cancel", null);

        var response = await owner.Client.PutAsync($"/api/reservations/{reservation.Id}/confirm", null);

        await AssertErrorAsync(response, HttpStatusCode.BadRequest, "Only pending reservations can be confirmed");
    }

    // ---------- PUT /api/reservations/{id}/cancel ----------

    [Fact]
    public async Task Cancel_ByOwningCustomer_ShouldReturnCancelledReservation()
    {
        var owner = await TestApi.RegisterOwnerAsync(_factory);
        var service = await TestApi.CreateServiceAsync(owner);
        var customer = await TestApi.RegisterCustomerAsync(_factory);
        var reservation = await BookOkAsync(customer, service, TestApi.FutureSlot());

        var response = await customer.Client.PutAsync($"/api/reservations/{reservation.Id}/cancel", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = (await response.Content.ReadFromJsonAsync<ReservationResponse>())!;
        body.Id.Should().Be(reservation.Id);
        body.Status.Should().Be("Cancelled");
        body.CustomerEmail.Should().Be(customer.Email);
        body.ServiceName.Should().Be(service.Name);
    }

    [Fact]
    public async Task Cancel_ByOwnerOfCompany_ShouldReturnCancelledReservation()
    {
        var owner = await TestApi.RegisterOwnerAsync(_factory);
        var service = await TestApi.CreateServiceAsync(owner);
        var customer = await TestApi.RegisterCustomerAsync(_factory);
        var reservation = await BookOkAsync(customer, service, TestApi.FutureSlot());

        var response = await owner.Client.PutAsync($"/api/reservations/{reservation.Id}/cancel", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadFromJsonAsync<ReservationResponse>())!.Status.Should().Be("Cancelled");
    }

    [Fact]
    public async Task Cancel_ConfirmedReservation_ShouldSucceed()
    {
        var owner = await TestApi.RegisterOwnerAsync(_factory);
        var service = await TestApi.CreateServiceAsync(owner);
        var customer = await TestApi.RegisterCustomerAsync(_factory);
        var reservation = await BookOkAsync(customer, service, TestApi.FutureSlot());
        await owner.Client.PutAsync($"/api/reservations/{reservation.Id}/confirm", null);

        var response = await customer.Client.PutAsync($"/api/reservations/{reservation.Id}/cancel", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Cancel_ByAnotherCustomer_ShouldReturnForbiddenAndLeaveReservationPending()
    {
        var owner = await TestApi.RegisterOwnerAsync(_factory);
        var service = await TestApi.CreateServiceAsync(owner);
        var customer = await TestApi.RegisterCustomerAsync(_factory);
        var stranger = await TestApi.RegisterCustomerAsync(_factory);
        var reservation = await BookOkAsync(customer, service, TestApi.FutureSlot());

        var response = await stranger.Client.PutAsync($"/api/reservations/{reservation.Id}/cancel", null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await ListAsync(customer)).Items.Single().Status.Should().Be("Pending");
    }

    [Fact]
    public async Task Cancel_ByOwnerOfAnotherCompany_ShouldReturnForbiddenAndLeaveReservationPending()
    {
        var owner = await TestApi.RegisterOwnerAsync(_factory);
        var otherOwner = await TestApi.RegisterOwnerAsync(_factory);
        var service = await TestApi.CreateServiceAsync(owner);
        var customer = await TestApi.RegisterCustomerAsync(_factory);
        var reservation = await BookOkAsync(customer, service, TestApi.FutureSlot());

        var response = await otherOwner.Client.PutAsync($"/api/reservations/{reservation.Id}/cancel", null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await ListAsync(customer)).Items.Single().Status.Should().Be("Pending");
    }

    [Fact]
    public async Task Cancel_UnknownReservation_ShouldReturnNotFound()
    {
        var customer = await TestApi.RegisterCustomerAsync(_factory);

        var response = await customer.Client.PutAsync($"/api/reservations/{Guid.NewGuid()}/cancel", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Cancel_AlreadyCancelledReservation_ShouldReturnBadRequest()
    {
        var owner = await TestApi.RegisterOwnerAsync(_factory);
        var service = await TestApi.CreateServiceAsync(owner);
        var customer = await TestApi.RegisterCustomerAsync(_factory);
        var reservation = await BookOkAsync(customer, service, TestApi.FutureSlot());
        await customer.Client.PutAsync($"/api/reservations/{reservation.Id}/cancel", null);

        var response = await customer.Client.PutAsync($"/api/reservations/{reservation.Id}/cancel", null);

        await AssertErrorAsync(response, HttpStatusCode.BadRequest, "Reservation cannot be cancelled");
    }
}
