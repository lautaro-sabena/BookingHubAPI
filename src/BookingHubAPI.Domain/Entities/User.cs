namespace BookingHubAPI.Domain.Entities;

public enum UserRole
{
    Owner,
    Customer
}

public class User : BaseEntity
{
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public Guid? CompanyId { get; set; }
    public Company? Company { get; set; }
}
