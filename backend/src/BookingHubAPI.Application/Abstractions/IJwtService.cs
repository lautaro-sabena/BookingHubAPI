using System.Security.Claims;

namespace BookingHubAPI.Application.Abstractions;

/// <summary>Issues and validates access tokens; implemented by the infrastructure layer.</summary>
public interface IJwtService
{
    string GenerateToken(Guid userId, string email, string role, Guid? companyId = null);
    ClaimsPrincipal? ValidateToken(string token);
}
