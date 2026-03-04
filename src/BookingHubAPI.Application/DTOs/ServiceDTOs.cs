using System.ComponentModel.DataAnnotations;

namespace BookingHubAPI.Application.DTOs;

public record ServiceRequest(
    [Required][MaxLength(100)] string Name, 
    [MaxLength(500)] string? Description, 
    [Required][Range(1, 480)] int DurationMinutes, 
    [Required][Range(0, 99999.99)] decimal Price);

public record ServiceResponse(Guid Id, string Name, string? Description, int DurationMinutes, decimal Price, bool IsActive, Guid CompanyId);

public record ServiceUpdateRequest(
    [MaxLength(100)] string? Name, 
    [MaxLength(500)] string? Description, 
    [Range(1, 480)] int? DurationMinutes, 
    [Range(0, 99999.99)] decimal? Price, 
    bool? IsActive);

public record PagedResult<T>(IEnumerable<T> Items, int TotalCount, int Page, int PageSize, int TotalPages);
