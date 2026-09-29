using BookingHubAPI.Domain.Entities;
using BookingHubAPI.Domain.Scheduling;
using FluentAssertions;

namespace BookingHubAPI.UnitTests.Domain;

public class BookingScheduleTests
{
    private static readonly DateOnly MondayDate = new(2030, 1, 7);
    private static readonly DateTimeOffset Monday = new(2030, 1, 7, 0, 0, 0, TimeSpan.Zero);

    private static readonly TimeZoneInfo Utc = TimeZoneInfo.Utc;
    private static readonly TimeZoneInfo BuenosAires = TimeZoneInfo.FindSystemTimeZoneById("America/Argentina/Buenos_Aires");
    private static readonly TimeZoneInfo Madrid = TimeZoneInfo.FindSystemTimeZoneById("Europe/Madrid");

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

    // ---------- time zone resolution and stored values ----------

    [Theory]
    [InlineData("UTC")]
    [InlineData("America/Argentina/Buenos_Aires")]
    [InlineData("Europe/Madrid")]
    public void ResolveTimeZone_FindsIanaIds(string id) =>
        BookingSchedule.ResolveTimeZone(id).Id.Should().Be(TimeZoneInfo.FindSystemTimeZoneById(id).Id);

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Not/AZone")]
    public void ResolveTimeZone_FallsBackToUtcForMissingOrUnknownIds(string? id) =>
        BookingSchedule.ResolveTimeZone(id).Should().Be(TimeZoneInfo.Utc);

    [Fact]
    public void ToInstant_TreatsUnspecifiedAndUtcStoredValuesAsUtc()
    {
        var wall = new DateTime(2030, 1, 7, 12, 0, 0);

        BookingSchedule.ToInstant(DateTime.SpecifyKind(wall, DateTimeKind.Unspecified))
            .Should().Be(new DateTimeOffset(2030, 1, 7, 12, 0, 0, TimeSpan.Zero));
        BookingSchedule.ToInstant(DateTime.SpecifyKind(wall, DateTimeKind.Utc))
            .Should().Be(new DateTimeOffset(2030, 1, 7, 12, 0, 0, TimeSpan.Zero));
    }

    [Fact]
    public void ToInstant_ConvertsLocalKindToTheSameInstant()
    {
        var local = DateTime.SpecifyKind(new DateTime(2030, 1, 7, 12, 0, 0, DateTimeKind.Utc), DateTimeKind.Utc).ToLocalTime();

        BookingSchedule.ToInstant(local).Should().Be(new DateTimeOffset(2030, 1, 7, 12, 0, 0, TimeSpan.Zero));
    }

    [Fact]
    public void ToLocal_ExpressesTheStoredInstantWithTheZoneOffset()
    {
        var stored = new DateTime(2030, 1, 7, 12, 0, 0, DateTimeKind.Utc);

        var local = BookingSchedule.ToLocal(stored, BuenosAires);

        local.Should().Be(new DateTimeOffset(2030, 1, 7, 9, 0, 0, TimeSpan.FromHours(-3)));
        local.Offset.Should().Be(TimeSpan.FromHours(-3));
        local.Hour.Should().Be(9);
    }

    // ---------- windows ----------

    [Fact]
    public void WindowOn_UtcZone_AppliesTheClockTimesToTheCalendarDay()
    {
        var (open, close) = BookingSchedule.WindowOn(Hours(DayOfWeek.Monday, 9, 17), MondayDate, Utc);

        open.Should().Be(Monday.AddHours(9));
        close.Should().Be(Monday.AddHours(17));
    }

    [Fact]
    public void WindowOn_NonUtcZone_ConvertsTheLocalWindowToUtcInstants()
    {
        var (open, close) = BookingSchedule.WindowOn(Hours(DayOfWeek.Monday, 9, 17), MondayDate, BuenosAires);

        // Buenos Aires is UTC-03:00 all year: 09:00 local is 12:00Z.
        open.Should().Be(Monday.AddHours(12));
        close.Should().Be(Monday.AddHours(20));
    }

    [Fact]
    public void WindowOn_EndOfDayCloseIsTheNextLocalMidnight()
    {
        var (_, close) = BookingSchedule.WindowOn(Hours(DayOfWeek.Monday, 9, 24), MondayDate, BuenosAires);

        close.Should().Be(new DateTimeOffset(2030, 1, 8, 3, 0, 0, TimeSpan.Zero));
    }

    [Fact]
    public void WindowOn_FollowsDaylightSavingTimeAcrossTheChange()
    {
        // Madrid springs forward on Sunday 2030-03-31 (02:00 -> 03:00): +01:00 before, +02:00 after.
        var hours = Hours(DayOfWeek.Saturday, 9, 17);
        var sunday = Hours(DayOfWeek.Sunday, 9, 17);

        var (saturdayOpen, _) = BookingSchedule.WindowOn(hours, new DateOnly(2030, 3, 30), Madrid);
        var (sundayOpen, _) = BookingSchedule.WindowOn(sunday, new DateOnly(2030, 3, 31), Madrid);

        saturdayOpen.Should().Be(new DateTimeOffset(2030, 3, 30, 8, 0, 0, TimeSpan.Zero));
        sundayOpen.Should().Be(new DateTimeOffset(2030, 3, 31, 7, 0, 0, TimeSpan.Zero));
    }

    [Fact]
    public void WindowOn_ADayWithASpringForwardIsOneHourShorter()
    {
        var (open, close) = BookingSchedule.WindowOn(Hours(DayOfWeek.Sunday, 0, 24), new DateOnly(2030, 3, 31), Madrid);

        (close - open).Should().Be(TimeSpan.FromHours(23));
    }

    [Fact]
    public void WindowOn_ADayWithAFallBackIsOneHourLonger()
    {
        // Madrid falls back on Sunday 2030-10-27 (03:00 -> 02:00).
        var (open, close) = BookingSchedule.WindowOn(Hours(DayOfWeek.Sunday, 0, 24), new DateOnly(2030, 10, 27), Madrid);

        (close - open).Should().Be(TimeSpan.FromHours(25));
    }

    [Fact]
    public void WindowOn_AnOpeningTimeSkippedBySpringForwardMovesForwardByTheGap()
    {
        // 02:30 does not exist on 2030-03-31 in Madrid; it becomes 03:30 (+02:00) = 01:30Z.
        var hours = new WorkingHours { DayOfWeek = DayOfWeek.Sunday, IsActive = true, StartTime = new TimeSpan(2, 30, 0), EndTime = new TimeSpan(6, 0, 0) };

        var (open, _) = BookingSchedule.WindowOn(hours, new DateOnly(2030, 3, 31), Madrid);

        open.Should().Be(new DateTimeOffset(2030, 3, 31, 1, 30, 0, TimeSpan.Zero));
    }

    [Fact]
    public void WindowOn_AnOpeningTimeRepeatedByFallBackMeansTheFirstOccurrence()
    {
        // 02:30 happens twice on 2030-10-27 in Madrid; the first one is still +02:00 = 00:30Z.
        var hours = new WorkingHours { DayOfWeek = DayOfWeek.Sunday, IsActive = true, StartTime = new TimeSpan(2, 30, 0), EndTime = new TimeSpan(6, 0, 0) };

        var (open, _) = BookingSchedule.WindowOn(hours, new DateOnly(2030, 10, 27), Madrid);

        open.Should().Be(new DateTimeOffset(2030, 10, 27, 0, 30, 0, TimeSpan.Zero));
    }

    // ---------- opening hours ----------

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

        BookingSchedule.IsWithinOpeningHours(schedule, Utc, Monday.AddHours(start), Monday.AddHours(end))
            .Should().Be(expected);
    }

    [Theory]
    [InlineData(12, 13, true)]    // 09:00-10:00 local
    [InlineData(11, 12, false)]   // 08:00-09:00 local: before opening
    [InlineData(19.5, 20.5, false)] // 16:30-17:30 local: ends after closing
    [InlineData(9, 10, false)]    // 06:00-07:00 local: the UTC wall clock would have accepted it
    public void IsWithinOpeningHours_ReadsTheWindowInTheCompanyTimeZone(double startUtcHour, double endUtcHour, bool expected)
    {
        var schedule = new[] { Hours(DayOfWeek.Monday, 9, 17) };

        BookingSchedule.IsWithinOpeningHours(schedule, BuenosAires, Monday.AddHours(startUtcHour), Monday.AddHours(endUtcHour))
            .Should().Be(expected);
    }

    [Fact]
    public void IsWithinOpeningHours_UsesTheLocalDayNotTheUtcDay()
    {
        // 2030-01-08T01:00Z is still Monday 22:00 in Buenos Aires.
        var schedule = new[] { Hours(DayOfWeek.Monday, 9, 24) };
        var start = new DateTimeOffset(2030, 1, 8, 1, 0, 0, TimeSpan.Zero);

        BookingSchedule.IsWithinOpeningHours(schedule, BuenosAires, start, start.AddHours(1)).Should().BeTrue();
        BookingSchedule.IsWithinOpeningHours(schedule, Utc, start, start.AddHours(1)).Should().BeFalse();
    }

    [Fact]
    public void IsWithinOpeningHours_IgnoresTheOffsetTheInstantIsExpressedIn()
    {
        var schedule = new[] { Hours(DayOfWeek.Monday, 9, 17) };
        var asUtc = Monday.AddHours(12);
        var asLocal = asUtc.ToOffset(TimeSpan.FromHours(-3));

        BookingSchedule.IsWithinOpeningHours(schedule, BuenosAires, asUtc, asUtc.AddHours(1)).Should().BeTrue();
        BookingSchedule.IsWithinOpeningHours(schedule, BuenosAires, asLocal, asLocal.AddHours(1)).Should().BeTrue();
    }

    [Fact]
    public void IsWithinOpeningHours_IsFalseOnAClosedOrInactiveDay()
    {
        var schedule = new[] { Hours(DayOfWeek.Monday, 9, 17, isActive: false) };

        BookingSchedule.IsWithinOpeningHours(schedule, Utc, Monday.AddHours(10), Monday.AddHours(11)).Should().BeFalse();
        BookingSchedule.IsWithinOpeningHours(Array.Empty<WorkingHours>(), Utc, Monday.AddHours(10), Monday.AddHours(11)).Should().BeFalse();
    }

    [Fact]
    public void IsWithinOpeningHours_IsFalseWhenTheIntervalCrossesMidnight()
    {
        var schedule = new[] { Hours(DayOfWeek.Monday, 9, 24) };

        BookingSchedule.IsWithinOpeningHours(schedule, Utc, Monday.AddHours(23), Monday.AddHours(25)).Should().BeFalse();
    }

    [Fact]
    public void IsWithinOpeningHours_AcrossASpringForwardUsesTheNewOffset()
    {
        // Sunday 2030-03-31 in Madrid, open 09:00-11:00 local = 07:00Z-09:00Z (+02:00 after the change).
        var schedule = new[] { Hours(DayOfWeek.Sunday, 9, 11) };
        var nineLocal = new DateTimeOffset(2030, 3, 31, 7, 0, 0, TimeSpan.Zero);
        var nineWithOldOffset = new DateTimeOffset(2030, 3, 31, 8, 0, 0, TimeSpan.Zero);

        BookingSchedule.IsWithinOpeningHours(schedule, Madrid, nineLocal, nineLocal.AddHours(1)).Should().BeTrue();
        // 08:00Z is 10:00 local: still inside, but a start at 06:00Z (08:00 local) is not.
        BookingSchedule.IsWithinOpeningHours(schedule, Madrid, nineWithOldOffset, nineWithOldOffset.AddHours(1)).Should().BeTrue();
        BookingSchedule.IsWithinOpeningHours(schedule, Madrid, nineLocal.AddHours(-1), nineLocal).Should().BeFalse();
    }
}
