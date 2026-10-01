using BookingHubAPI.Domain.Entities;

namespace BookingHubAPI.Domain.Scheduling;

/// <summary>
/// The single rule for when a company can be booked: on a day it is open only inside that
/// day's active working-hours window. Availability (which slots to offer) and booking (which
/// requests to accept) both read it from here so they can never disagree.
/// </summary>
/// <remarks>
/// Working hours are a wall clock: the company time zone is not applied, and the <c>Kind</c> of the
/// compared <see cref="DateTime"/> values is ignored (see the availability characterization tests).
/// </remarks>
public static class BookingSchedule
{
    /// <summary>The active entry for <paramref name="day"/> (the first, if legacy data holds several), or null when closed.</summary>
    public static WorkingHours? FindActiveHours(IEnumerable<WorkingHours> schedule, DayOfWeek day) =>
        schedule.FirstOrDefault(wh => wh.DayOfWeek == day && wh.IsActive);

    /// <summary>The opening and closing moments of <paramref name="hours"/> on the calendar day of <paramref name="date"/>.</summary>
    public static (DateTime Open, DateTime Close) WindowOn(WorkingHours hours, DateTime date) =>
        (date.Date.Add(hours.StartTime), date.Date.Add(hours.EndTime));

    /// <summary>True when [<paramref name="start"/>, <paramref name="end"/>] lies inside the active window of the day it starts on.</summary>
    public static bool IsWithinOpeningHours(IEnumerable<WorkingHours> schedule, DateTime start, DateTime end)
    {
        var hours = FindActiveHours(schedule, start.DayOfWeek);
        if (hours == null)
        {
            return false;
        }

        var (open, close) = WindowOn(hours, start);
        return start >= open && end <= close;
    }
}
