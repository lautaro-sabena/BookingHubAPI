using BookingHubAPI.Application.DTOs;
using BookingHubAPI.Domain.Entities;
using BookingHubAPI.Domain.Interfaces;
using BookingHubAPI.Infrastructure.Auth;
using BookingHubAPI.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;

namespace BookingHubAPI.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IUserRepository _userRepository;
    private readonly ICompanyRepository _companyRepository;
    private readonly IJwtService _jwtService;
    private readonly INotificationService _notificationService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IUserRepository userRepository,
        ICompanyRepository companyRepository,
        IJwtService jwtService,
        INotificationService notificationService,
        ILogger<AuthController> logger)
    {
        _userRepository = userRepository;
        _companyRepository = companyRepository;
        _jwtService = jwtService;
        _notificationService = notificationService;
        _logger = logger;
    }

    [HttpPost("register")]
    public async Task<ActionResult<TokenResponse>> Register([FromBody] RegisterRequest request)
    {
        if (!Enum.TryParse<UserRole>(request.Role, true, out var role))
        {
            return BadRequest(new { error = "Invalid role. Must be 'Owner' or 'Customer'" });
        }

        if (await _userRepository.ExistsAsync(request.Email))
        {
            return BadRequest(new { error = "Email already registered" });
        }

        var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

        var user = new User
        {
            Email = request.Email,
            PasswordHash = passwordHash,
            Role = role
        };

        var createdUser = await _userRepository.CreateAsync(user);

        if (role == UserRole.Owner)
        {
            var company = new Company
            {
                Name = $"{request.Email}'s Company",
                OwnerId = createdUser.Id,
                TimeZone = "UTC"
            };
            var createdCompany = await _companyRepository.CreateAsync(company);
            createdUser.CompanyId = createdCompany.Id;
            await _userRepository.UpdateAsync(createdUser);
        }

        var token = _jwtService.GenerateToken(createdUser.Id, createdUser.Email, createdUser.Role.ToString(), createdUser.CompanyId);

        return Ok(new TokenResponse(token, createdUser.Id, createdUser.Email, createdUser.Role.ToString()));
    }

    [HttpPost("login")]
    public async Task<ActionResult<TokenResponse>> Login([FromBody] LoginRequest request)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email);

        if (user == null)
        {
            return Unauthorized(new { error = "Invalid email or password" });
        }

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            return Unauthorized(new { error = "Invalid email or password" });
        }

        var token = _jwtService.GenerateToken(user.Id, user.Email, user.Role.ToString(), user.CompanyId);

        return Ok(new TokenResponse(token, user.Id, user.Email, user.Role.ToString()));
    }
}
