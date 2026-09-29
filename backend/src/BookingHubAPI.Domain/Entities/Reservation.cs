using BookingHubAPI.Domain.Exceptions;

namespace BookingHubAPI.Domain.Entities;

public enum ReservationStatus
{
    Pending,
    Confirmed,
    Cancelled,
    Completed
}

public class Reservation : BaseEntity
{
    public const string OnlyPendingCanBeConfirmedMessage = "Only pending reservations can be confirmed";
    public const string CannotBeCancelledMessage = "Reservation cannot be cancelled";

    public Guid CustomerId { get; set; }
    public User Customer { get; set; } = null!;
    public Guid ServiceId { get; set; }
    public Service Service { get; set; } = null!;
    public Guid CompanyId { get; set; }
    public Company Company { get; set; } = null!;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    // The setter stays public only because tests and EF materialization write it directly; production code
    // changes the status through Confirm() and Cancel(), which enforce the transitions.
    public ReservationStatus Status { get; set; } = ReservationStatus.Pending;
    public string? Notes { get; set; }

    /// <summary>Only a pending reservation can be confirmed.</summary>
    public bool CanBeConfirmed => Status == ReservationStatus.Pending;

    /// <summary>A pending or confirmed reservation can be cancelled; cancelled and completed ones are final.</summary>
    public bool CanBeCancelled => Status is ReservationStatus.Pending or ReservationStatus.Confirmed;

    /// <exception cref="DomainException">The reservation is not pending.</exception>
    public void Confirm()
    {
        if (!CanBeConfirmed)
        {
            throw new DomainException(OnlyPendingCanBeConfirmedMessage);
        }

        Status = ReservationStatus.Confirmed;
    }

    /// <exception cref="DomainException">The reservation is already cancelled or completed.</exception>
    public void Cancel()
    {
        if (!CanBeCancelled)
        {
            throw new DomainException(CannotBeCancelledMessage);
        }

        Status = ReservationStatus.Cancelled;
    }
}
