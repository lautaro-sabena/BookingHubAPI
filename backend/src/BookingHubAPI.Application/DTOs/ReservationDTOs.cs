using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using BookingHubAPI.Domain.Entities;

namespace BookingHubAPI.Application.DTOs;

/// <param name="StartTime">The slot start as ISO 8601 WITH a UTC offset (a slot's <c>startTime</c> is posted back unchanged).</param>
public record ReservationRequest(
    [Required] Guid ServiceId,
    [Required][property: JsonConverter(typeof(OffsetRequiredDateTimeOffsetConverter))] DateTimeOffset StartTime,
    [MaxLength(500)] string? Notes);

/// <param name="StartTime">In the company's local time, with that zone's offset at that moment.</param>
/// <param name="EndTime">In the company's local time, with that zone's offset at that moment.</param>
public record ReservationResponse(
    Guid Id,
    Guid CustomerId,
    string CustomerEmail,
    Guid ServiceId,
    string ServiceName,
    int ServiceDuration,
    DateTimeOffset StartTime,
    DateTimeOffset EndTime,
    string Status,
    string? Notes,
    DateTime CreatedAt);

public record ReservationUpdateRequest(ReservationStatus Status);

public enum ReservationStatusDto
{
    Pending,
    Confirmed,
    Cancelled,
    Completed
}
