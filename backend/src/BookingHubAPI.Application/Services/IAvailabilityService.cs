using BookingHubAPI.Application.Common;
using BookingHubAPI.Application.DTOs;

namespace BookingHubAPI.Application.Services;

/// <summary>Availability use case. Expected failures are returned as <see cref="Result"/> errors.</summary>
public interface IAvailabilityService
{
    /// <summary>
    /// Returns the free slots of a service on the calendar day of <paramref name="date"/>: consecutive
    /// slots of the service duration inside the company's active working hours, minus those that overlap
    /// a non-cancelled reservation of the same service. A closed day yields an empty list; an unknown or
    /// inactive service or company is a not-found error.
    /// </summary>
    Task<Result<IReadOnlyList<AvailableSlotResponse>>> GetAvailableSlotsAsync(Guid serviceId, DateTime date);
}
