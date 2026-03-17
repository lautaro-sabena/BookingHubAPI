namespace BookingHubAPI.Domain.Entities;

public class WorkingHours : BaseEntity
{
    public Guid CompanyId { get; set; }
    public Company Company { get; set; } = null!;
    public DayOfWeek DayOfWeek { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public bool IsActive { get; set; } = true;
}
