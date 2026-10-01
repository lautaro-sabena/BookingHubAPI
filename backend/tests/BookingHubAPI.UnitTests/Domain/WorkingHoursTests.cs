using BookingHubAPI.Domain.Entities;
using BookingHubAPI.Domain.Exceptions;
using FluentAssertions;

namespace BookingHubAPI.UnitTests.Domain;

public class WorkingHoursTests
{
    private static TimeSpan H(double hours) => TimeSpan.FromHours(hours);

    [Theory]
    [InlineData(9, 17, true)]
    [InlineData(0, 24, true)]
    [InlineData(17, 9, false)]   // inactive days may keep an inverted window
    public void Validate_AcceptsValidEntries(double start, double end, bool isActive)
    {
        WorkingHours.Validate(DayOfWeek.Monday, H(start), H(end), isActive).Should().BeNull();
    }

    [Theory]
    [InlineData(17, 9)]
    [InlineData(9, 9)]
    public void Validate_RejectsAnActiveDayThatDoesNotOpenBeforeItCloses(double start, double end)
    {
        WorkingHours.Validate(DayOfWeek.Monday, H(start), H(end), isActive: true)
            .Should().Be("Start time must be before end time for Monday");
    }

    [Theory]
    [InlineData(-1, 9, true)]
    [InlineData(9, 24.5, true)]
    [InlineData(25, 26, true)]
    [InlineData(-1, 9, false)]
    [InlineData(9, 24.5, false)]
    public void Validate_RejectsTimesOutsideTheDay(double start, double end, bool isActive)
    {
        WorkingHours.Validate(DayOfWeek.Monday, H(start), H(end), isActive)
            .Should().Be("Working hours must be within 00:00 and 24:00 for Monday");
    }

    [Fact]
    public void Create_ReturnsTheEntryForAValidWindow()
    {
        var companyId = Guid.NewGuid();

        var hours = WorkingHours.Create(companyId, DayOfWeek.Friday, H(8), H(12), isActive: true);

        hours.CompanyId.Should().Be(companyId);
        hours.DayOfWeek.Should().Be(DayOfWeek.Friday);
        hours.StartTime.Should().Be(H(8));
        hours.EndTime.Should().Be(H(12));
        hours.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Create_ForAnInvalidWindow_Throws()
    {
        var act = () => WorkingHours.Create(Guid.NewGuid(), DayOfWeek.Friday, H(12), H(8), isActive: true);

        act.Should().Throw<DomainException>().WithMessage("Start time must be before end time for Friday");
    }
}
