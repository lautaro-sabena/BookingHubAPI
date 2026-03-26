namespace BookingHubAPI.Domain.Entities;

public class Favorite : BaseEntity
{
    public Guid CustomerId { get; set; }
    public User Customer { get; set; } = null!;
    public Guid ServiceId { get; set; }
    public Service Service { get; set; } = null!;
}