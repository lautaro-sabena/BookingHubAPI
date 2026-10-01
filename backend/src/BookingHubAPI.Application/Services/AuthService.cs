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
    private readonly IUnitOfWork _unitOfWork;

    public AuthService(
        IUserRepository userRepository,
        ICompanyRepository companyRepository,
        IPasswordHasher passwordHasher,
        IJwtService jwtService,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _companyRepository = companyRepository;
        _passwordHasher = passwordHasher;
        _jwtService = jwtService;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<TokenResponse>> RegisterAsync(RegisterRequest request)
    {
        if (!TryParseRole(request.Role, out var role))
        {
            return Error.Validation("Invalid role. Must be 'Owner' or 'Customer'");
        }

        var email = NormalizeEmail(request.Email);

        if (await _userRepository.ExistsAsync(email))
        {
            return Error.Validation("Email already registered");
        }

        // Hashed once, outside the transaction: the delegate below may be re-run after a transient failure.
        var passwordHash = _passwordHasher.Hash(request.Password);
        User createdUser = null!;

        // The user, its default company and the link between them are written atomically, so a failure
        // never leaves an owner without a company.
        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            createdUser = await _userRepository.CreateAsync(new User
            {
                Email = email,
                PasswordHash = passwordHash,
                Role = role
            });

            if (role == UserRole.Owner)
            {
                var createdCompany = await _companyRepository.CreateAsync(new Company
                {
                    Name = $"{email}'s Company",
                    OwnerId = createdUser.Id,
                    TimeZone = "UTC"
                });
                createdUser.CompanyId = createdCompany.Id;
                await _userRepository.UpdateAsync(createdUser);
            }
        });

        return ToTokenResponse(createdUser);
    }

    public async Task<Result<TokenResponse>> LoginAsync(LoginRequest request)
    {
        var user = await _userRepository.GetByEmailAsync(NormalizeEmail(request.Email));

        // Verify against a dummy hash when the e-mail is unknown so both paths cost one hash
        // verification and response time does not reveal which e-mails are registered.
        var passwordHash = user?.PasswordHash ?? DummyPasswordHash();
        var passwordMatches = _passwordHasher.Verify(request.Password, passwordHash);

        if (user == null || !passwordMatches)
        {
            return Error.Unauthorized("Invalid email or password");
        }

        return ToTokenResponse(user);
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    /// <summary>Only the role names are accepted; numeric strings that <see cref="Enum.TryParse{TEnum}(string?, bool, out TEnum)"/> would take are not.</summary>
    private static bool TryParseRole(string value, out UserRole role)
    {
        var name = Enum.GetNames<UserRole>().FirstOrDefault(n => n.Equals(value?.Trim(), StringComparison.OrdinalIgnoreCase));
        role = name == null ? default : Enum.Parse<UserRole>(name);
        return name != null;
    }

    // Hashed once per process with the configured hasher, so its cost always matches real hashes.
    private static string? _dummyPasswordHash;

    private string DummyPasswordHash() => _dummyPasswordHash ??= _passwordHasher.Hash("dummy-password-for-timing");

    private TokenResponse ToTokenResponse(User user)
    {
        var role = user.Role.ToString();
        var token = _jwtService.GenerateToken(user.Id, user.Email, role, user.CompanyId);
        return new TokenResponse(token, user.Id, user.Email, role);
    }
}
