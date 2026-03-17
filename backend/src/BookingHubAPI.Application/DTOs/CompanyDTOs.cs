using System.ComponentModel.DataAnnotations;

namespace BookingHubAPI.Application.DTOs;

public record CompanyRequest(
    [Required][MaxLength(100)] string Name, 
    [MaxLength(1000)] string? Description, 
    [Required][MaxLength(50)] string TimeZone);

public record CompanyResponse(Guid Id, string Name, string? Description, string TimeZone, bool IsActive, Guid OwnerId);

public record CompanyUpdateRequest(
    [MaxLength(100)] string? Name, 
    [MaxLength(1000)] string? Description, 
    [MaxLength(50)] string? TimeZone);
