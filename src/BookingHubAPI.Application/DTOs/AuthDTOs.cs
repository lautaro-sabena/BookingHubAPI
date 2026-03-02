namespace BookingHubAPI.Application.DTOs;

public record RegisterRequest(string Email, string Password, string Role);

public record LoginRequest(string Email, string Password);

public record TokenResponse(string Token, Guid UserId, string Email, string Role);

public record UserDto(Guid Id, string Email, string Role, Guid? CompanyId);
