using BookingHubAPI.Domain.Exceptions;

namespace BookingHubAPI.Domain.Entities;

public class WorkingHours : BaseEntity
{
    private static readonly TimeSpan EndOfDay = TimeSpan.FromHours(24);

    public Guid CompanyId { get; set; }
    public Company Company { get; set; } = null!;
    public DayOfWeek DayOfWeek { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// The reason a schedule entry is invalid, or null when it is valid. Both times must lie within one day
    /// (00:00 to 24:00); an active day must also open before it closes. An inactive day only keeps its times,
    /// so their order is not checked.
    /// </summary>
    public static string? Validate(DayOfWeek dayOfWeek, TimeSpan startTime, TimeSpan endTime, bool isActive)
    {
        if (startTime < TimeSpan.Zero || startTime > EndOfDay || endTime < TimeSpan.Zero || endTime > EndOfDay)
        {
            return $"Working hours must be within 00:00 and 24:00 for {dayOfWeek}";
        }

        if (isActive && startTime >= endTime)
        {
            return $"Start time must be before end time for {dayOfWeek}";
        }

        return null;
    }

    /// <exception cref="DomainException">The entry breaks <see cref="Validate"/>.</exception>
    public static WorkingHours Create(Guid companyId, DayOfWeek dayOfWeek, TimeSpan startTime, TimeSpan endTime, bool isActive)
    {
        var problem = Validate(dayOfWeek, startTime, endTime, isActive);
        if (problem != null)
        {
            throw new DomainException(problem);
        }

        return new WorkingHours
        {
            CompanyId = companyId,
            DayOfWeek = dayOfWeek,
            StartTime = startTime,
            EndTime = endTime,
            IsActive = isActive
        };
    }
}
