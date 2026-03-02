namespace BookingHubAPI.Application.DTOs;

public record AvailableSlotResponse(DateTime StartTime, DateTime EndTime, bool IsAvailable);

public record AvailabilityQueryRequest(Guid ServiceId, DateTime Date);
