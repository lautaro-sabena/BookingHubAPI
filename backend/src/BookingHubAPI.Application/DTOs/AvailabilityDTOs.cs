namespace BookingHubAPI.Application.DTOs;

/// <param name="StartTime">In the company's local time, with that zone's offset at that moment.</param>
/// <param name="EndTime">In the company's local time, with that zone's offset at that moment.</param>
public record AvailableSlotResponse(DateTimeOffset StartTime, DateTimeOffset EndTime, bool IsAvailable);

public record AvailabilityQueryRequest(Guid ServiceId, DateTime Date);

public record WorkingHoursRequest(DayOfWeek DayOfWeek, TimeSpan StartTime, TimeSpan EndTime, bool IsActive);

public record WorkingHoursResponse(DayOfWeek DayOfWeek, TimeSpan StartTime, TimeSpan EndTime, bool IsActive);
