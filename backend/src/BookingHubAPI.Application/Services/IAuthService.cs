using BookingHubAPI.Application.Common;
using BookingHubAPI.Application.DTOs;

namespace BookingHubAPI.Application.Services;

/// <summary>Registration and login use cases. Expected failures are returned as <see cref="Result"/> errors, never thrown.</summary>
public interface IAuthService
{
    /// <summary>Creates a user (and, for owners, their company) and returns an access token.</summary>
    Task<Result<TokenResponse>> RegisterAsync(RegisterRequest request);

    /// <summary>Returns an access token; wrong password and unknown e-mail are indistinguishable.</summary>
    Task<Result<TokenResponse>> LoginAsync(LoginRequest request);
}
