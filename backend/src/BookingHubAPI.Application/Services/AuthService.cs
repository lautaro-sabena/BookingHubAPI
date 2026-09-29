using BookingHubAPI.Application.Abstractions;
using BookingHubAPI.Application.Common;
using BookingHubAPI.Application.DTOs;
using BookingHubAPI.Domain.Entities;
using BookingHubAPI.Domain.Interfaces;

namespace BookingHubAPI.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly ICompanyRepository _companyRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtService _jwtService;

    public AuthService(
        IUserRepository userRepository,
        ICompanyRepository companyRepository,
        IPasswordHasher passwordHasher,
        IJwtService jwtService)
    {
        _userRepository = userRepository;
        _companyRepository = companyRepository;
        _passwordHasher = passwordHasher;
        _jwtService = jwtService;
    }

    public async Task<Result<TokenResponse>> RegisterAsync(RegisterRequest request)
    {
        if (!Enum.TryParse<UserRole>(request.Role, true, out var role))
        {
            return Error.Validation("Invalid role. Must be 'Owner' or 'Customer'");
        }

        if (await _userRepository.ExistsAsync(request.Email))
        {
            return Error.Validation("Email already registered");
        }

        var user = new User
        {
            Email = request.Email,
            PasswordHash = _passwordHasher.Hash(request.Password),
            Role = role
        };

        var createdUser = await _userRepository.CreateAsync(user);

        // Not atomic: the user, the company and the user update are three separate writes
        // (there is no unit of work yet). Tracked for the transaction work in T9.
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

        return ToTokenResponse(createdUser);
    }

    public async Task<Result<TokenResponse>> LoginAsync(LoginRequest request)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email);

        if (user == null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            return Error.Unauthorized("Invalid email or password");
        }

        return ToTokenResponse(user);
    }

    private TokenResponse ToTokenResponse(User user)
    {
        var role = user.Role.ToString();
        var token = _jwtService.GenerateToken(user.Id, user.Email, role, user.CompanyId);
        return new TokenResponse(token, user.Id, user.Email, role);
    }
}
