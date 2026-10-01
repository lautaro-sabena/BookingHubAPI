using BookingHubAPI.Application.Common;
using BookingHubAPI.Application.DTOs;

namespace BookingHubAPI.Application.Services;

/// <summary>Reservation use cases. Expected failures are returned as <see cref="Result"/> errors, never thrown.</summary>
public interface IReservationService
{
    /// <summary>Lists the caller's reservations: the company's for an owner, the caller's own otherwise.</summary>
    Task<Result<PagedResult<ReservationResponse>>> GetReservationsAsync(
        Guid userId, int page, int pageSize, string? status);

    Task<Result<ReservationResponse>> CreateReservationAsync(Guid userId, ReservationRequest request);

    /// <summary>Confirms a pending reservation of the owner's company.</summary>
    Task<Result<ReservationResponse>> ConfirmReservationAsync(Guid userId, Guid reservationId);

    /// <summary>Cancels a reservation the caller owns (customer) or that belongs to the caller's company (owner).</summary>
    Task<Result<ReservationResponse>> CancelReservationAsync(Guid userId, Guid reservationId);
}
