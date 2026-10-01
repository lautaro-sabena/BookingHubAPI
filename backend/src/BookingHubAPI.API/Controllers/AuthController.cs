using System.Security.Claims;
using BookingHubAPI.API.Authentication;
using BookingHubAPI.Application.DTOs;
using BookingHubAPI.Application.Services;
using BookingHubAPI.API.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookingHubAPI.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly SessionCookie _sessionCookie;

    public AuthController(IAuthService authService, SessionCookie sessionCookie)
    {
        _authService = authService;
        _sessionCookie = sessionCookie;
    }

    /// <summary>Creates the account and signs it in: the JWT is set in the httpOnly session cookie, the body is the user.</summary>
    [HttpPost("register")]
    [RequireCsrfHeader]
    public async Task<ActionResult<UserDto>> Register([FromBody] RegisterRequest request)
    {
        return this.ToActionResult(await _authService.RegisterAsync(request), SignIn);
    }

    /// <summary>Signs in: the JWT is set in the httpOnly session cookie, the body is the user.</summary>
    [HttpPost("login")]
    [RequireCsrfHeader]
    public async Task<ActionResult<UserDto>> Login([FromBody] LoginRequest request)
    {
        return this.ToActionResult(await _authService.LoginAsync(request), SignIn);
    }

    /// <summary>Clears the session cookie. Idempotent; works whether or not a session exists.</summary>
    [HttpPost("logout")]
    [RequireCsrfHeader]
    public IActionResult Logout()
    {
        _sessionCookie.Delete(Response);
        return NoContent();
    }

    /// <summary>The signed-in user (from the token claims), so the frontend can restore the session on load.</summary>
    [HttpGet("me")]
    [Authorize]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public ActionResult<UserDto> Me()
    {
        var companyClaim = User.FindFirst("companyId")?.Value;
        return Ok(new UserDto(
            User.GetUserId(),
            User.FindFirst(ClaimTypes.Email)?.Value ?? string.Empty,
            User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty,
            Guid.TryParse(companyClaim, out var companyId) ? companyId : null));
    }

    private ActionResult SignIn(AuthSession session)
    {
        _sessionCookie.Append(Response, session.Token);
        return Ok(new UserDto(session.UserId, session.Email, session.Role, session.CompanyId));
    }
}
