using BookingHubAPI.Domain.Entities;

namespace BookingHubAPI.Domain.Scheduling;

/// <summary>
/// The single rule for when a company can be booked: on a day it is open only inside that
/// day's active working-hours window. Availability (which slots to offer) and booking (which
/// requests to accept) both read it from here so they can never disagree.
/// </summary>
/// <remarks>
/// Working hours are wall-clock times in the company's time zone. Every window is converted to
/// absolute instants (<see cref="DateTimeOffset"/>) through <see cref="TimeZoneInfo"/>, so a
/// daylight-saving change shortens or lengthens the window instead of shifting it. Reservations are
/// stored as UTC instants: <see cref="ToInstant(DateTime)"/> reads a stored value back regardless of
/// the <see cref="DateTime.Kind"/> the persistence provider gave it.
/// </remarks>
public static class BookingSchedule
{
    /// <summary>
    /// The time zone named by <paramref name="timeZoneId"/> (an IANA id). Ids are validated when a company
    /// is written; a legacy row holding an unknown id is treated as UTC rather than failing every request.
    /// </summary>
    public static TimeZoneInfo ResolveTimeZone(string? timeZoneId) =>
        !string.IsNullOrWhiteSpace(timeZoneId) && TimeZoneInfo.TryFindSystemTimeZoneById(timeZoneId, out var zone)
            ? zone
            : TimeZoneInfo.Utc;

    /// <summary>
    /// A stored reservation time as an instant. Stored values are UTC; a <c>Local</c> kind (what Npgsql's
    /// legacy timestamp mode hands back) is converted, an <c>Unspecified</c> kind is taken as UTC.
    /// </summary>
    public static DateTimeOffset ToInstant(DateTime stored) => stored.Kind switch
    {
        DateTimeKind.Unspecified => new DateTimeOffset(DateTime.SpecifyKind(stored, DateTimeKind.Utc)),
        _ => new DateTimeOffset(stored.ToUniversalTime())
    };

    /// <summary>The instant <paramref name="stored"/> expressed in the local time (and offset) of <paramref name="zone"/>.</summary>
    public static DateTimeOffset ToLocal(DateTime stored, TimeZoneInfo zone) =>
        TimeZoneInfo.ConvertTime(ToInstant(stored), zone);

    /// <summary>The active entry for <paramref name="day"/> (the first, if legacy data holds several), or null when closed.</summary>
    public static WorkingHours? FindActiveHours(IEnumerable<WorkingHours> schedule, DayOfWeek day) =>
        schedule.FirstOrDefault(wh => wh.DayOfWeek == day && wh.IsActive);

    /// <summary>The opening and closing instants of <paramref name="hours"/> on the company-local calendar <paramref name="date"/>.</summary>
    public static (DateTimeOffset Open, DateTimeOffset Close) WindowOn(WorkingHours hours, DateOnly date, TimeZoneInfo zone)
    {
        var midnight = date.ToDateTime(TimeOnly.MinValue);
        return (LocalToInstant(midnight.Add(hours.StartTime), zone), LocalToInstant(midnight.Add(hours.EndTime), zone));
    }

    /// <summary>
    /// True when [<paramref name="start"/>, <paramref name="end"/>] lies inside the active window of the
    /// company-local day the interval starts on.
    /// </summary>
    public static bool IsWithinOpeningHours(
        IEnumerable<WorkingHours> schedule, TimeZoneInfo zone, DateTimeOffset start, DateTimeOffset end)
    {
        var localStart = TimeZoneInfo.ConvertTime(start, zone);
        var hours = FindActiveHours(schedule, localStart.DayOfWeek);
        if (hours == null)
        {
            return false;
        }

        var (open, close) = WindowOn(hours, DateOnly.FromDateTime(localStart.DateTime), zone);
        return start >= open && end <= close;
    }

    /// <summary>
    /// A wall-clock time as an instant. A time skipped by a spring-forward change moves forward by the gap;
    /// a time repeated by a fall-back change means its first occurrence.
    /// </summary>
    private static DateTimeOffset LocalToInstant(DateTime local, TimeZoneInfo zone)
    {
        var unspecified = DateTime.SpecifyKind(local, DateTimeKind.Unspecified);
        var offset = zone.IsAmbiguousTime(unspecified)
            ? zone.GetAmbiguousTimeOffsets(unspecified).Max()
            : zone.GetUtcOffset(unspecified);
        return new DateTimeOffset(unspecified, offset).ToUniversalTime();
    }
}
