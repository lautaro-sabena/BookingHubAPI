using BookingHubAPI.Application.Common;
using BookingHubAPI.Application.DTOs;

namespace BookingHubAPI.Application.Services;

/// <summary>Working-hours use cases of the owner's own company. Expected failures are returned as <see cref="Result"/> errors.</summary>
public interface IWorkingHoursService
{
    /// <summary>Returns the schedule as one entry per weekday; unconfigured days are inactive with the default window.</summary>
    Task<Result<IReadOnlyList<WorkingHoursResponse>>> GetWorkingHoursAsync(Guid userId);

    /// <summary>
    /// Replaces the company's schedule with the request (inactive days keep their times) and returns the saved
    /// seven-day schedule. Rejects out-of-range or duplicate days, times outside 00:00-24:00 and, for active
    /// days, start not before end.
    /// </summary>
    Task<Result<IReadOnlyList<WorkingHoursResponse>>> ReplaceWorkingHoursAsync(Guid userId, IReadOnlyList<WorkingHoursRequest> requests);
}
