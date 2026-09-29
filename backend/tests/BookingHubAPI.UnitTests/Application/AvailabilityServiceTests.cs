using BookingHubAPI.Application.Common;
using BookingHubAPI.Application.Services;
using BookingHubAPI.Domain.Entities;
using BookingHubAPI.Domain.Interfaces;
using FluentAssertions;
using Moq;
using ServiceEntity = BookingHubAPI.Domain.Entities.Service;

namespace BookingHubAPI.UnitTests.Application;

public class AvailabilityServiceTests
{
    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public DateTimeOffset Now { get; set; } = now;

        public override DateTimeOffset GetUtcNow() => Now;
    }

    private static readonly DateTime Monday = new(2030, 1, 7);

    /// <summary>A UTC instant on <see cref="Monday"/> (the default test company is in UTC).</summary>
    private static DateTimeOffset At(double hour) => new(DateTime.SpecifyKind(Monday.AddHours(hour), DateTimeKind.Utc));

    private readonly Mock<IServiceRepository> _services = new();
    private readonly Mock<ICompanyRepository> _companies = new();
    private readonly Mock<IReservationRepository> _reservations = new();
    private readonly FixedTimeProvider _clock = new(new DateTimeOffset(2030, 1, 1, 0, 0, 0, TimeSpan.Zero));
    private readonly AvailabilityService _sut;

    private readonly Company _company = new() { Id = Guid.NewGuid(), Name = "Acme", IsActive = true };
    private readonly ServiceEntity _service;

    public AvailabilityServiceTests()
    {
        _service = new ServiceEntity
        {
            Id = Guid.NewGuid(), CompanyId = _company.Id, Name = "Haircut", DurationMinutes = 60, IsActive = true
        };
        _services.Setup(s => s.GetByIdAsync(_service.Id)).ReturnsAsync(_service);
        _companies.Setup(c => c.GetByIdWithWorkingHoursAsync(_company.Id)).ReturnsAsync(_company);
        _sut = new AvailabilityService(_services.Object, _companies.Object, _reservations.Object, _clock);
    }

    private void Open(DayOfWeek day, int startHour, int endHour, bool isActive = true) =>
        _company.WorkingHours.Add(new WorkingHours
        {
            CompanyId = _company.Id, DayOfWeek = day, IsActive = isActive,
            StartTime = TimeSpan.FromHours(startHour), EndTime = TimeSpan.FromHours(endHour)
        });

    [Fact]
    public async Task UnknownService_ReturnsNotFound()
    {
        var result = await _sut.GetAvailableSlotsAsync(Guid.NewGuid(), Monday);

        result.Error!.Kind.Should().Be(ErrorKind.NotFound);
        result.Error.Message.Should().Be("Service not found or inactive");
    }

    [Fact]
    public async Task InactiveService_ReturnsNotFound()
    {
        _service.IsActive = false;

        var result = await _sut.GetAvailableSlotsAsync(_service.Id, Monday);

        result.Error!.Kind.Should().Be(ErrorKind.NotFound);
    }

    [Fact]
    public async Task InactiveCompany_ReturnsNotFound()
    {
        _company.IsActive = false;

        var result = await _sut.GetAvailableSlotsAsync(_service.Id, Monday);

        result.Error!.Kind.Should().Be(ErrorKind.NotFound);
        result.Error.Message.Should().Be("Company not found or inactive");
    }

    [Fact]
    public async Task ClosedDay_ReturnsNoSlotsWithoutQueryingReservations()
    {
        Open(DayOfWeek.Monday, 9, 12, isActive: false);

        var result = await _sut.GetAvailableSlotsAsync(_service.Id, Monday);

        result.Value.Should().BeEmpty();
        _reservations.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task OpenDay_ReturnsConsecutiveSlotsInsideTheWindow()
    {
        Open(DayOfWeek.Monday, 9, 12);

        var result = await _sut.GetAvailableSlotsAsync(_service.Id, Monday.AddHours(15));

        result.Value.Select(s => s.StartTime).Should().Equal(At(9), At(10), At(11));
        result.Value.Select(s => s.EndTime).Should().Equal(At(10), At(11), At(12));
        result.Value.Should().OnlyContain(s => s.IsAvailable);
    }

    [Fact]
    public async Task SlotWithAConflict_IsOmitted()
    {
        Open(DayOfWeek.Monday, 9, 12);
        _reservations
            .Setup(r => r.HasConflictAsync(_company.Id, _service.Id, Monday.AddHours(10), Monday.AddHours(11), null))
            .ReturnsAsync(true);

        var result = await _sut.GetAvailableSlotsAsync(_service.Id, Monday);

        result.Value.Select(s => s.StartTime).Should().Equal(At(9), At(11));
    }

    [Fact]
    public async Task PartialLastSlot_IsDropped()
    {
        _service.DurationMinutes = 45;
        Open(DayOfWeek.Monday, 9, 11);

        var result = await _sut.GetAvailableSlotsAsync(_service.Id, Monday);

        result.Value.Select(s => s.StartTime).Should().Equal(At(9), At(9.75));
    }

    [Fact]
    public async Task NonPositiveDuration_ReturnsNoSlotsInsteadOfLooping()
    {
        _service.DurationMinutes = 0;
        Open(DayOfWeek.Monday, 9, 12);

        var result = await _sut.GetAvailableSlotsAsync(_service.Id, Monday);

        result.Value.Should().BeEmpty();
    }

    // ---------- time zones ----------

    [Fact]
    public async Task CompanyInAnotherTimeZone_ReturnsLocalSlotsWithTheZoneOffset()
    {
        _company.TimeZone = "America/Argentina/Buenos_Aires";
        Open(DayOfWeek.Monday, 9, 11);

        var result = await _sut.GetAvailableSlotsAsync(_service.Id, Monday);

        result.Value.Select(s => s.StartTime).Should().Equal(At(12), At(13));
        result.Value.Select(s => s.StartTime.Offset).Should().OnlyContain(o => o == TimeSpan.FromHours(-3));
        result.Value.Select(s => s.StartTime.Hour).Should().Equal(9, 10);
    }

    [Fact]
    public async Task ConflictsAreCheckedOnUtcInstants()
    {
        _company.TimeZone = "America/Argentina/Buenos_Aires";
        Open(DayOfWeek.Monday, 9, 11);
        _reservations
            .Setup(r => r.HasConflictAsync(_company.Id, _service.Id, At(12).UtcDateTime, At(13).UtcDateTime, null))
            .ReturnsAsync(true);

        var result = await _sut.GetAvailableSlotsAsync(_service.Id, Monday);

        result.Value.Select(s => s.StartTime).Should().Equal(At(13));
    }

    [Fact]
    public async Task DaylightSavingDay_ReturnsSlotsAtTheOffsetInForceAtEachMoment()
    {
        _company.TimeZone = "Europe/Madrid";
        Open(DayOfWeek.Sunday, 9, 11);

        var result = await _sut.GetAvailableSlotsAsync(_service.Id, new DateTime(2030, 3, 31));

        result.Value.Select(s => s.StartTime).Should().Equal(
            new DateTimeOffset(2030, 3, 31, 9, 0, 0, TimeSpan.FromHours(2)),
            new DateTimeOffset(2030, 3, 31, 10, 0, 0, TimeSpan.FromHours(2)));
        result.Value.Select(s => s.StartTime.Offset).Should().OnlyContain(o => o == TimeSpan.FromHours(2));
    }

    [Fact]
    public async Task UnknownStoredTimeZone_FallsBackToUtc()
    {
        _company.TimeZone = "Not/AZone";
        Open(DayOfWeek.Monday, 9, 10);

        var result = await _sut.GetAvailableSlotsAsync(_service.Id, Monday);

        result.Value.Select(s => s.StartTime).Should().Equal(At(9));
    }

    // ---------- past slots ----------

    [Fact]
    public async Task SlotsThatAlreadyStarted_AreOmitted()
    {
        Open(DayOfWeek.Monday, 9, 12);
        _clock.Now = At(10.5);

        var result = await _sut.GetAvailableSlotsAsync(_service.Id, Monday);

        result.Value.Select(s => s.StartTime).Should().Equal(At(11));
    }

    [Fact]
    public async Task ASlotStartingExactlyNow_IsStillOffered()
    {
        Open(DayOfWeek.Monday, 9, 12);
        _clock.Now = At(10);

        var result = await _sut.GetAvailableSlotsAsync(_service.Id, Monday);

        result.Value.Select(s => s.StartTime).Should().Equal(At(10), At(11));
    }

    [Fact]
    public async Task ADayInThePast_ReturnsNoSlotsWithoutQueryingReservations()
    {
        Open(DayOfWeek.Monday, 9, 12);
        _clock.Now = At(0).AddDays(1);

        var result = await _sut.GetAvailableSlotsAsync(_service.Id, Monday);

        result.Value.Should().BeEmpty();
        _reservations.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task PastIsJudgedInAbsoluteTime_NotByTheCompanyWallClock()
    {
        // 12:30Z is 09:30 in Buenos Aires: the 09:00 local (12:00Z) slot is past, 10:00 local is not.
        _company.TimeZone = "America/Argentina/Buenos_Aires";
        Open(DayOfWeek.Monday, 9, 11);
        _clock.Now = At(12.5);

        var result = await _sut.GetAvailableSlotsAsync(_service.Id, Monday);

        result.Value.Select(s => s.StartTime).Should().Equal(At(13));
    }
}
