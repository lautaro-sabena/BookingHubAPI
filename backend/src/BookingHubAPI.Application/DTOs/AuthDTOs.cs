using System.ComponentModel.DataAnnotations;

namespace BookingHubAPI.Application.DTOs;

public record RegisterRequest(
    [Required][EmailAddress][MaxLength(256)] string Email, 
    [Required][MinLength(6)][MaxLength(100)] string Password, 
    [Required][MaxLength(20)] string Role);

public record LoginRequest(
    [Required][EmailAddress][MaxLength(256)] string Email, 
    [Required][MaxLength(100)] string Password);

public record TokenResponse(string Token, Guid UserId, string Email, string Role);

public record UserDto(Guid Id, string Email, string Role, Guid? CompanyId);
