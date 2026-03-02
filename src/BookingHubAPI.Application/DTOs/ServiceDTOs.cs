namespace BookingHubAPI.Application.DTOs;

public record ServiceRequest(string Name, string? Description, int DurationMinutes, decimal Price);

public record ServiceResponse(Guid Id, string Name, string? Description, int DurationMinutes, decimal Price, bool IsActive, Guid CompanyId);

public record ServiceUpdateRequest(string? Name, string? Description, int? DurationMinutes, decimal? Price, bool? IsActive);

public record PagedResult<T>(IEnumerable<T> Items, int TotalCount, int Page, int PageSize, int TotalPages);
