using System.ComponentModel.DataAnnotations;
using BookingHubAPI.Domain.Entities;

namespace BookingHubAPI.Application.DTOs;

public record ReservationRequest(
    [Required] Guid ServiceId, 
    [Required] DateTime StartTime, 
    [MaxLength(500)] string? Notes);

public record ReservationResponse(
    Guid Id,
    Guid CustomerId,
    Guid ServiceId,
    string ServiceName,
    int ServiceDuration,
    DateTime StartTime,
    DateTime EndTime,
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
