using BookingHubAPI.Application.Abstractions;
using BookingHubAPI.Application.Common;
using BookingHubAPI.Application.DTOs;
using BookingHubAPI.Application.Services;
using BookingHubAPI.Application.Validators;
using BookingHubAPI.Domain.Entities;
using BookingHubAPI.Domain.Interfaces;
using FluentAssertions;
using Moq;
using ServiceEntity = BookingHubAPI.Domain.Entities.Service;

namespace BookingHubAPI.UnitTests.Application;

public class ReservationServiceTests
{
    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private static readonly DateTimeOffset Now = new(2030, 1, 1, 8, 0, 0, TimeSpan.Zero);

    private readonly Mock<IReservationRepository> _reservations = new();
    private readonly Mock<IServiceRepository> _services = new();
    private readonly Mock<ICompanyRepository> _companies = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<INotificationService> _notifications = new();
    private readonly ReservationService _sut;

    private readonly Company _company = new() { Id = Guid.NewGuid(), Name = "Acme", IsActive = true };
    private readonly Company _otherCompany = new() { Id = Guid.NewGuid(), Name = "Other", IsActive = true };
    private readonly User _customer;
    private readonly User _otherCustomer;
    private readonly User _owner;
    private readonly User _otherOwner;
    private readonly ServiceEntity _service;

    public ReservationServiceTests()
    {
        _customer = new User { Id = Guid.NewGuid(), Email = "c@test.com", Role = UserRole.Customer };
        _otherCustomer = new User { Id = Guid.NewGuid(), Email = "c2@test.com", Role = UserRole.Customer };
        _owner = new User { Id = Guid.NewGuid(), Email = "o@test.com", Role = UserRole.Owner, CompanyId = _company.Id };
        _otherOwner = new User { Id = Guid.NewGuid(), Email = "o2@test.com", Role = UserRole.Owner, CompanyId = _otherCompany.Id };
        _service = new ServiceEntity
        {
            Id = Guid.NewGuid(), CompanyId = _company.Id, Company = _company,
            Name = "Haircut", DurationMinutes = 45, IsActive = true
        };

        foreach (var user in new[] { _customer, _otherCustomer, _owner, _otherOwner })
        {
            _users.Setup(u => u.GetByIdAsync(user.Id)).ReturnsAsync(user);
        }

        _services.Setup(s => s.GetByIdAsync(_service.Id)).ReturnsAsync(_service);
        // Open every day 08:00-18:00; the fixed "now" bookings below (08:00 + 45 minutes) fall inside.
        foreach (var day in Enum.GetValues<DayOfWeek>())
        {
            _company.WorkingHours.Add(new WorkingHours
            {
                CompanyId = _company.Id, DayOfWeek = day, IsActive = true,
                StartTime = TimeSpan.FromHours(8), EndTime = TimeSpan.FromHours(18)
            });
        }

        _companies.Setup(c => c.GetByIdWithWorkingHoursAsync(_company.Id)).ReturnsAsync(_company);
        _reservations.Setup(r => r.CreateAsync(It.IsAny<Reservation>()))
            .ReturnsAsync((Reservation r) => { r.Id = Guid.NewGuid(); r.CreatedAt = Now.UtcDateTime; return r; });
        _reservations.Setup(r => r.UpdateAsync(It.IsAny<Reservation>()))
            .ReturnsAsync((Reservation r) => r);

        var validator = new ReservationRequestValidator(new FixedTimeProvider(Now));
        _sut = new ReservationService(
            _reservations.Object, _services.Object, _companies.Object,
            _users.Object, _notifications.Object, validator);
    }

    private Reservation ExistingReservation(ReservationStatus status) => new()
    {
        Id = Guid.NewGuid(),
        CustomerId = _customer.Id,
        Customer = _customer,
        ServiceId = _service.Id,
        Service = _service,
        CompanyId = _company.Id,
        Company = _company,
        StartTime = Now.UtcDateTime.AddDays(1),
        EndTime = Now.UtcDateTime.AddDays(1).AddMinutes(45),
        Status = status
    };

    private Reservation GivenReservation(ReservationStatus status)
    {
        var reservation = ExistingReservation(status);
        _reservations.Setup(r => r.GetByIdAsync(reservation.Id)).ReturnsAsync(reservation);
        return reservation;
    }

    private static void AssertFailure<T>(Result<T> result, ErrorKind kind)
    {
        result.IsFailure.Should().BeTrue();
        result.Error!.Kind.Should().Be(kind);
    }

    // ---------- create: conflict detection ----------

    [Fact]
    public async Task Create_ChecksConflictOverServiceDurationWindow()
    {
        var start = Now.UtcDateTime.AddDays(1);

        var result = await _sut.CreateReservationAsync(_customer.Id, new ReservationRequest(_service.Id, start, null));

        result.IsSuccess.Should().BeTrue();
        _reservations.Verify(r => r.HasConflictAsync(
            _company.Id, _service.Id, start, start.AddMinutes(45), null), Times.Once);
    }

    [Fact]
    public async Task Create_WhenSlotTaken_ReturnsConflictWithoutCreatingOrNotifying()
    {
        _reservations
            .Setup(r => r.HasConflictAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), null))
            .ReturnsAsync(true);

        var result = await _sut.CreateReservationAsync(
            _customer.Id, new ReservationRequest(_service.Id, Now.UtcDateTime.AddDays(1), null));

        AssertFailure(result, ErrorKind.Conflict);
        result.Error!.Message.Should().Be("Time slot is not available");
        _reservations.Verify(r => r.CreateAsync(It.IsAny<Reservation>()), Times.Never);
        _notifications.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Create_ReportsConflictBeforePastValidation()
    {
        _reservations
            .Setup(r => r.HasConflictAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), null))
            .ReturnsAsync(true);

        var result = await _sut.CreateReservationAsync(
            _customer.Id, new ReservationRequest(_service.Id, Now.UtcDateTime.AddDays(-1), null));

        AssertFailure(result, ErrorKind.Conflict);
    }

    // ---------- create: other rules ----------

    [Fact]
    public async Task Create_InThePast_ReturnsValidationError()
    {
        var result = await _sut.CreateReservationAsync(
            _customer.Id, new ReservationRequest(_service.Id, Now.UtcDateTime.AddMinutes(-1), null));

        AssertFailure(result, ErrorKind.Validation);
        result.Error!.Message.Should().Be("Cannot book in the past");
        _reservations.Verify(r => r.CreateAsync(It.IsAny<Reservation>()), Times.Never);
    }

    [Fact]
    public async Task Create_AtExactlyNow_IsAllowed()
    {
        var result = await _sut.CreateReservationAsync(
            _customer.Id, new ReservationRequest(_service.Id, Now.UtcDateTime, null));

        result.IsSuccess.Should().BeTrue();
    }

    // ---------- create: booking hours ----------

    private static DateTime OnWednesday(double hour) => new DateTime(2030, 1, 2).AddHours(hour);

    [Theory]
    [InlineData(7.0)]    // before opening (08:00)
    [InlineData(7.5)]    // starts before opening, ends inside
    [InlineData(17.5)]   // starts inside, 45 minutes end after closing (18:00)
    [InlineData(18.0)]   // starts at closing
    public async Task Create_OutsideWorkingHours_ReturnsValidationErrorWithoutCreating(double startHour)
    {
        var result = await _sut.CreateReservationAsync(
            _customer.Id, new ReservationRequest(_service.Id, OnWednesday(startHour), null));

        AssertFailure(result, ErrorKind.Validation);
        result.Error!.Message.Should().Be(ReservationService.OutsideWorkingHoursMessage);
        _reservations.Verify(r => r.CreateAsync(It.IsAny<Reservation>()), Times.Never);
        _notifications.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(8.0)]    // exactly at opening
    [InlineData(17.25)]  // ends exactly at closing
    public async Task Create_TouchingTheWorkingHoursBoundary_IsAllowed(double startHour)
    {
        var result = await _sut.CreateReservationAsync(
            _customer.Id, new ReservationRequest(_service.Id, OnWednesday(startHour), null));

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Create_OnAnInactiveDay_ReturnsValidationError()
    {
        _company.WorkingHours.Single(wh => wh.DayOfWeek == DayOfWeek.Wednesday).IsActive = false;

        var result = await _sut.CreateReservationAsync(
            _customer.Id, new ReservationRequest(_service.Id, OnWednesday(10), null));

        AssertFailure(result, ErrorKind.Validation);
        result.Error!.Message.Should().Be(ReservationService.OutsideWorkingHoursMessage);
    }

    [Fact]
    public async Task Create_WhenTheCompanyHasNoSchedule_ReturnsValidationError()
    {
        _company.WorkingHours.Clear();

        var result = await _sut.CreateReservationAsync(
            _customer.Id, new ReservationRequest(_service.Id, OnWednesday(10), null));

        AssertFailure(result, ErrorKind.Validation);
        result.Error!.Message.Should().Be(ReservationService.OutsideWorkingHoursMessage);
    }

    [Fact]
    public async Task Create_ReportsPastValidationBeforeWorkingHours()
    {
        // 2030-01-01 07:00 is both before "now" (08:00) and outside the working hours.
        var result = await _sut.CreateReservationAsync(
            _customer.Id, new ReservationRequest(_service.Id, new DateTime(2030, 1, 1, 7, 0, 0), null));

        result.Error!.Message.Should().Be("Cannot book in the past");
    }

    [Fact]
    public async Task Create_WithValidRequest_PersistsPendingReservationAndNotifies()
    {
        var start = Now.UtcDateTime.AddDays(1);

        var result = await _sut.CreateReservationAsync(_customer.Id, new ReservationRequest(_service.Id, start, "hi"));

        result.IsSuccess.Should().BeTrue();
        var response = result.Value;
        response.CustomerId.Should().Be(_customer.Id);
        response.CustomerEmail.Should().Be(_customer.Email);
        response.ServiceName.Should().Be("Haircut");
        response.ServiceDuration.Should().Be(45);
        response.StartTime.Should().Be(start);
        response.EndTime.Should().Be(start.AddMinutes(45));
        response.Status.Should().Be("Pending");
        response.Notes.Should().Be("hi");
        _notifications.Verify(n => n.SendReservationCreatedAsync(response.Id, _customer.Email, "Acme"), Times.Once);
    }

    [Fact]
    public async Task Create_ForUnknownOrInactiveService_ReturnsValidationError()
    {
        var inactive = new ServiceEntity { Id = Guid.NewGuid(), CompanyId = _company.Id, IsActive = false };
        _services.Setup(s => s.GetByIdAsync(inactive.Id)).ReturnsAsync(inactive);

        var unknown = await _sut.CreateReservationAsync(
            _customer.Id, new ReservationRequest(Guid.NewGuid(), Now.UtcDateTime.AddDays(1), null));
        var inactiveResult = await _sut.CreateReservationAsync(
            _customer.Id, new ReservationRequest(inactive.Id, Now.UtcDateTime.AddDays(1), null));

        AssertFailure(unknown, ErrorKind.Validation);
        AssertFailure(inactiveResult, ErrorKind.Validation);
        unknown.Error!.Message.Should().Be("Service not found or inactive");
    }

    [Fact]
    public async Task Create_ForInactiveCompany_ReturnsValidationError()
    {
        _company.IsActive = false;

        var result = await _sut.CreateReservationAsync(
            _customer.Id, new ReservationRequest(_service.Id, Now.UtcDateTime.AddDays(1), null));

        AssertFailure(result, ErrorKind.Validation);
        result.Error!.Message.Should().Be("Company not found or inactive");
    }

    [Fact]
    public async Task Create_ForUnknownUser_ReturnsNotFound()
    {
        var result = await _sut.CreateReservationAsync(
            Guid.NewGuid(), new ReservationRequest(_service.Id, Now.UtcDateTime.AddDays(1), null));

        AssertFailure(result, ErrorKind.NotFound);
    }

    // ---------- confirm: ownership and transitions ----------

    [Fact]
    public async Task Confirm_ByOwnerOfCompany_MovesPendingToConfirmedAndNotifies()
    {
        var reservation = GivenReservation(ReservationStatus.Pending);
        _users.Setup(u => u.GetByIdAsync(reservation.CustomerId)).ReturnsAsync(_customer);

        var result = await _sut.ConfirmReservationAsync(_owner.Id, reservation.Id);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be("Confirmed");
        result.Value.CustomerEmail.Should().Be(_customer.Email);
        _notifications.Verify(n => n.SendReservationConfirmedAsync(reservation.Id, _customer.Email), Times.Once);
    }

    [Theory]
    [InlineData(ReservationStatus.Confirmed)]
    [InlineData(ReservationStatus.Cancelled)]
    [InlineData(ReservationStatus.Completed)]
    public async Task Confirm_WhenNotPending_ReturnsValidationErrorAndDoesNotUpdate(ReservationStatus status)
    {
        var reservation = GivenReservation(status);

        var result = await _sut.ConfirmReservationAsync(_owner.Id, reservation.Id);

        AssertFailure(result, ErrorKind.Validation);
        result.Error!.Message.Should().Be("Only pending reservations can be confirmed");
        _reservations.Verify(r => r.UpdateAsync(It.IsAny<Reservation>()), Times.Never);
        reservation.Status.Should().Be(status);
    }

    [Fact]
    public async Task Confirm_AsCustomer_ReturnsForbidden()
    {
        var reservation = GivenReservation(ReservationStatus.Pending);

        var result = await _sut.ConfirmReservationAsync(_customer.Id, reservation.Id);

        AssertFailure(result, ErrorKind.Forbidden);
        reservation.Status.Should().Be(ReservationStatus.Pending);
    }

    [Fact]
    public async Task Confirm_ByOwnerWithoutCompany_ReturnsForbidden()
    {
        var reservation = GivenReservation(ReservationStatus.Pending);
        _owner.CompanyId = null;

        var result = await _sut.ConfirmReservationAsync(_owner.Id, reservation.Id);

        AssertFailure(result, ErrorKind.Forbidden);
    }

    [Fact]
    public async Task Confirm_ByOwnerOfOtherCompany_ReturnsNotFoundAndDoesNotUpdate()
    {
        var reservation = GivenReservation(ReservationStatus.Pending);

        var result = await _sut.ConfirmReservationAsync(_otherOwner.Id, reservation.Id);

        AssertFailure(result, ErrorKind.NotFound);
        _reservations.Verify(r => r.UpdateAsync(It.IsAny<Reservation>()), Times.Never);
        reservation.Status.Should().Be(ReservationStatus.Pending);
    }

    [Fact]
    public async Task Confirm_UnknownReservation_ReturnsNotFound()
    {
        var result = await _sut.ConfirmReservationAsync(_owner.Id, Guid.NewGuid());

        AssertFailure(result, ErrorKind.NotFound);
    }

    // ---------- cancel: ownership and transitions ----------

    [Theory]
    [InlineData(ReservationStatus.Pending)]
    [InlineData(ReservationStatus.Confirmed)]
    public async Task Cancel_ByOwningCustomer_CancelsPendingOrConfirmedAndNotifies(ReservationStatus status)
    {
        var reservation = GivenReservation(status);
        _users.Setup(u => u.GetByIdAsync(reservation.CustomerId)).ReturnsAsync(_customer);

        var result = await _sut.CancelReservationAsync(_customer.Id, reservation.Id);

        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be("Cancelled");
        _notifications.Verify(
            n => n.SendReservationCancelledAsync(reservation.Id, _customer.Email, "Reservation cancelled"), Times.Once);
    }

    [Theory]
    [InlineData(ReservationStatus.Cancelled)]
    [InlineData(ReservationStatus.Completed)]
    public async Task Cancel_WhenAlreadyFinished_ReturnsValidationErrorAndDoesNotUpdate(ReservationStatus status)
    {
        var reservation = GivenReservation(status);

        var result = await _sut.CancelReservationAsync(_customer.Id, reservation.Id);

        AssertFailure(result, ErrorKind.Validation);
        result.Error!.Message.Should().Be("Reservation cannot be cancelled");
        _reservations.Verify(r => r.UpdateAsync(It.IsAny<Reservation>()), Times.Never);
        reservation.Status.Should().Be(status);
    }

    [Fact]
    public async Task Cancel_ByOwnerWithoutCompany_ReturnsForbiddenAndDoesNotUpdate()
    {
        var reservation = GivenReservation(ReservationStatus.Pending);
        _owner.CompanyId = null;

        var result = await _sut.CancelReservationAsync(_owner.Id, reservation.Id);

        AssertFailure(result, ErrorKind.Forbidden);
        _reservations.Verify(r => r.UpdateAsync(It.IsAny<Reservation>()), Times.Never);
        reservation.Status.Should().Be(ReservationStatus.Pending);
    }

    [Fact]
    public async Task Cancel_ByOtherCustomer_ReturnsForbiddenAndDoesNotUpdate()
    {
        var reservation = GivenReservation(ReservationStatus.Pending);

        var result = await _sut.CancelReservationAsync(_otherCustomer.Id, reservation.Id);

        AssertFailure(result, ErrorKind.Forbidden);
        _reservations.Verify(r => r.UpdateAsync(It.IsAny<Reservation>()), Times.Never);
        reservation.Status.Should().Be(ReservationStatus.Pending);
    }

    [Fact]
    public async Task Cancel_ByOwnerOfCompany_Succeeds()
    {
        var reservation = GivenReservation(ReservationStatus.Pending);

        var result = await _sut.CancelReservationAsync(_owner.Id, reservation.Id);

        result.IsSuccess.Should().BeTrue();
        reservation.Status.Should().Be(ReservationStatus.Cancelled);
    }

    [Fact]
    public async Task Cancel_ByOwnerOfOtherCompany_ReturnsForbiddenAndDoesNotUpdate()
    {
        var reservation = GivenReservation(ReservationStatus.Pending);

        var result = await _sut.CancelReservationAsync(_otherOwner.Id, reservation.Id);

        AssertFailure(result, ErrorKind.Forbidden);
        _reservations.Verify(r => r.UpdateAsync(It.IsAny<Reservation>()), Times.Never);
        reservation.Status.Should().Be(ReservationStatus.Pending);
    }

    [Fact]
    public async Task Cancel_UnknownReservation_ReturnsNotFound()
    {
        var result = await _sut.CancelReservationAsync(_customer.Id, Guid.NewGuid());

        AssertFailure(result, ErrorKind.NotFound);
    }

    // ---------- list ----------

    [Fact]
    public async Task List_ForOwnerWithCompany_QueriesByCompany()
    {
        _reservations.Setup(r => r.GetByCompanyIdAsync(_company.Id, 2, 5, ReservationStatus.Pending))
            .ReturnsAsync(new[] { ExistingReservation(ReservationStatus.Pending) });
        _reservations.Setup(r => r.GetCountByCompanyIdAsync(_company.Id, ReservationStatus.Pending)).ReturnsAsync(6);

        var result = await _sut.GetReservationsAsync(_owner.Id, 2, 5, "pending");

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCount.Should().Be(6);
        result.Value.TotalPages.Should().Be(2);
        result.Value.Items.Should().ContainSingle();
        _reservations.Verify(r => r.GetByCustomerIdAsync(
            It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<ReservationStatus?>()), Times.Never);
    }

    [Fact]
    public async Task List_ForCustomerWithUnknownStatus_QueriesOwnReservationsWithoutFilter()
    {
        _reservations.Setup(r => r.GetByCustomerIdAsync(_customer.Id, 1, 10, null))
            .ReturnsAsync(Array.Empty<Reservation>());

        var result = await _sut.GetReservationsAsync(_customer.Id, 1, 10, "bogus");

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
        result.Value.TotalPages.Should().Be(0);
    }

    [Fact]
    public async Task List_ForUnknownUser_ReturnsNotFound()
    {
        var result = await _sut.GetReservationsAsync(Guid.NewGuid(), 1, 10, null);

        AssertFailure(result, ErrorKind.NotFound);
    }
}
