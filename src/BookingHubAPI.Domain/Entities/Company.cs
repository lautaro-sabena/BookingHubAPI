namespace BookingHubAPI.Domain.Entities;

public class Company : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string TimeZone { get; set; } = "UTC";
    public bool IsActive { get; set; } = true;
    public Guid OwnerId { get; set; }
    public User Owner { get; set; } = null!;
    public ICollection<Service> Services { get; set; } = new List<Service>();
    public ICollection<WorkingHours> WorkingHours { get; set; } = new List<WorkingHours>();
}
