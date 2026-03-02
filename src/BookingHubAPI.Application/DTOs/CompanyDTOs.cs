namespace BookingHubAPI.Application.DTOs;

public record CompanyRequest(string Name, string? Description, string TimeZone);

public record CompanyResponse(Guid Id, string Name, string? Description, string TimeZone, bool IsActive, Guid OwnerId);

public record CompanyUpdateRequest(string? Name, string? Description, string? TimeZone);
