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
    public Guid CustomerId { get; set; }
    public User Customer { get; set; } = null!;
    public Guid ServiceId { get; set; }
    public Service Service { get; set; } = null!;
    public Guid CompanyId { get; set; }
    public Company Company { get; set; } = null!;
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public ReservationStatus Status { get; set; } = ReservationStatus.Pending;
    public string? Notes { get; set; }
}
