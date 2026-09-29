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
    private static readonly DateTime Monday = new(2030, 1, 7);

    private readonly Mock<IServiceRepository> _services = new();
    private readonly Mock<ICompanyRepository> _companies = new();
    private readonly Mock<IReservationRepository> _reservations = new();
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
        _sut = new AvailabilityService(_services.Object, _companies.Object, _reservations.Object);
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

        result.Value.Select(s => s.StartTime).Should().Equal(Monday.AddHours(9), Monday.AddHours(10), Monday.AddHours(11));
        result.Value.Select(s => s.EndTime).Should().Equal(Monday.AddHours(10), Monday.AddHours(11), Monday.AddHours(12));
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

        result.Value.Select(s => s.StartTime).Should().Equal(Monday.AddHours(9), Monday.AddHours(11));
    }

    [Fact]
    public async Task PartialLastSlot_IsDropped()
    {
        _service.DurationMinutes = 45;
        Open(DayOfWeek.Monday, 9, 11);

        var result = await _sut.GetAvailableSlotsAsync(_service.Id, Monday);

        result.Value.Select(s => s.StartTime).Should().Equal(Monday.AddHours(9), Monday.AddHours(9.75));
    }

    [Fact]
    public async Task NonPositiveDuration_ReturnsNoSlotsInsteadOfLooping()
    {
        _service.DurationMinutes = 0;
        Open(DayOfWeek.Monday, 9, 12);

        var result = await _sut.GetAvailableSlotsAsync(_service.Id, Monday);

        result.Value.Should().BeEmpty();
    }
}
