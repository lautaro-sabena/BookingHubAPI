namespace BookingHubAPI.Application.DTOs;

public class FavoriteDto
{
    public Guid Id { get; set; }
    public Guid ServiceId { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public string? ServiceDescription { get; set; }
    public int DurationMinutes { get; set; }
    public decimal Price { get; set; }
    public Guid CompanyId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
}