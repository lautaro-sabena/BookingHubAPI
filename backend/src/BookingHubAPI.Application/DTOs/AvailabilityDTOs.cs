namespace BookingHubAPI.Application.DTOs;

public record AvailableSlotResponse(DateTime StartTime, DateTime EndTime, bool IsAvailable);

public record AvailabilityQueryRequest(Guid ServiceId, DateTime Date);

public record WorkingHoursRequest(DayOfWeek DayOfWeek, TimeSpan StartTime, TimeSpan EndTime, bool IsActive);

public record WorkingHoursResponse(DayOfWeek DayOfWeek, TimeSpan StartTime, TimeSpan EndTime, bool IsActive);
