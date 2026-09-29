using BookingHubAPI.Domain.Entities;
using BookingHubAPI.Domain.Scheduling;
using FluentAssertions;

namespace BookingHubAPI.UnitTests.Domain;

public class BookingScheduleTests
{
    private static readonly DateTime Monday = new(2030, 1, 7);

    private static WorkingHours Hours(DayOfWeek day, int startHour, int endHour, bool isActive = true) => new()
    {
        DayOfWeek = day, IsActive = isActive,
        StartTime = TimeSpan.FromHours(startHour), EndTime = TimeSpan.FromHours(endHour)
    };

    [Fact]
    public void FindActiveHours_IgnoresInactiveEntriesAndOtherDays()
    {
        var active = Hours(DayOfWeek.Monday, 9, 12);
        var schedule = new[]
        {
            Hours(DayOfWeek.Monday, 6, 7, isActive: false), active, Hours(DayOfWeek.Tuesday, 9, 12)
        };

        BookingSchedule.FindActiveHours(schedule, DayOfWeek.Monday).Should().BeSameAs(active);
        BookingSchedule.FindActiveHours(schedule, DayOfWeek.Wednesday).Should().BeNull();
    }

    [Fact]
    public void WindowOn_AppliesTheClockTimesToTheCalendarDay()
    {
        var (open, close) = BookingSchedule.WindowOn(Hours(DayOfWeek.Monday, 9, 17), Monday.AddHours(13));

        open.Should().Be(Monday.AddHours(9));
        close.Should().Be(Monday.AddHours(17));
    }

    [Theory]
    [InlineData(9, 10, true)]     // exactly at opening
    [InlineData(16, 17, true)]    // exactly ending at closing
    [InlineData(8, 9, false)]     // before opening
    [InlineData(8.5, 9.5, false)] // starts before opening
    [InlineData(16.5, 17.5, false)] // ends after closing
    [InlineData(17, 18, false)]   // after closing
    public void IsWithinOpeningHours_ChecksTheWholeIntervalAgainstTheWindow(double start, double end, bool expected)
    {
        var schedule = new[] { Hours(DayOfWeek.Monday, 9, 17) };

        BookingSchedule.IsWithinOpeningHours(schedule, Monday.AddHours(start), Monday.AddHours(end))
            .Should().Be(expected);
    }

    [Fact]
    public void IsWithinOpeningHours_IsFalseOnAClosedOrInactiveDay()
    {
        var schedule = new[] { Hours(DayOfWeek.Monday, 9, 17, isActive: false) };

        BookingSchedule.IsWithinOpeningHours(schedule, Monday.AddHours(10), Monday.AddHours(11)).Should().BeFalse();
        BookingSchedule.IsWithinOpeningHours(Array.Empty<WorkingHours>(), Monday.AddHours(10), Monday.AddHours(11)).Should().BeFalse();
    }

    [Fact]
    public void IsWithinOpeningHours_IsFalseWhenTheIntervalCrossesMidnight()
    {
        var schedule = new[] { Hours(DayOfWeek.Monday, 9, 24) };

        BookingSchedule.IsWithinOpeningHours(schedule, Monday.AddHours(23), Monday.AddHours(25)).Should().BeFalse();
    }

    [Fact]
    public void IsWithinOpeningHours_IgnoresTheDateTimeKind()
    {
        var schedule = new[] { Hours(DayOfWeek.Monday, 9, 17) };
        var start = DateTime.SpecifyKind(Monday.AddHours(10), DateTimeKind.Utc);

        BookingSchedule.IsWithinOpeningHours(schedule, start, start.AddHours(1)).Should().BeTrue();
    }
}
