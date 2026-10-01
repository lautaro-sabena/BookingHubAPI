using BookingHubAPI.Application.Abstractions;
using BookingHubAPI.Application.Common;
using BookingHubAPI.Application.DTOs;
using BookingHubAPI.Application.Services;
using BookingHubAPI.Domain.Entities;
using BookingHubAPI.Domain.Interfaces;
using FluentAssertions;
using Moq;

namespace BookingHubAPI.UnitTests.Application;

public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<ICompanyRepository> _companies = new();
    private readonly Mock<IPasswordHasher> _hasher = new();
    private readonly Mock<IJwtService> _jwt = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly AuthService _sut;

    public AuthServiceTests()
    {
        _hasher.Setup(h => h.Hash(It.IsAny<string>())).Returns((string p) => $"hash:{p}");
        _hasher.Setup(h => h.Verify(It.IsAny<string>(), It.IsAny<string>()))
            .Returns((string p, string hash) => hash == $"hash:{p}");
        _jwt.Setup(j => j.GenerateToken(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid?>()))
            .Returns("token");
        _users.Setup(u => u.CreateAsync(It.IsAny<User>()))
            .ReturnsAsync((User u) => { if (u.Id == Guid.Empty) u.Id = Guid.NewGuid(); return u; });
        _users.Setup(u => u.UpdateAsync(It.IsAny<User>())).ReturnsAsync((User u) => u);
        _companies.Setup(c => c.CreateAsync(It.IsAny<Company>()))
            .ReturnsAsync((Company c) => { if (c.Id == Guid.Empty) c.Id = Guid.NewGuid(); return c; });

        _unitOfWork.Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<Task>>(), It.IsAny<Func<Task<bool>>>()))
            .Returns((Func<Task> work, Func<Task<bool>> _) => work());

        _sut = new AuthService(_users.Object, _companies.Object, _hasher.Object, _jwt.Object, _unitOfWork.Object);
    }

    // ---------- RegisterAsync ----------

    [Theory]
    [InlineData("Admin")]
    [InlineData("")]
    [InlineData("0")]
    [InlineData("1")]
    [InlineData("5")]
    public async Task Register_WithUnknownRole_ShouldReturnValidationErrorAndWriteNothing(string role)
    {
        var result = await _sut.RegisterAsync(new RegisterRequest("a@test.com", "Password123!", role));

        result.Error!.Kind.Should().Be(ErrorKind.Validation);
        result.Error.Message.Should().Be("Invalid role. Must be 'Owner' or 'Customer'");
        _users.Verify(u => u.CreateAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task Register_WithInvalidRoleAndExistingEmail_ShouldReportTheRoleFirst()
    {
        _users.Setup(u => u.ExistsAsync("a@test.com")).ReturnsAsync(true);

        var result = await _sut.RegisterAsync(new RegisterRequest("a@test.com", "Password123!", "Nope"));

        result.Error!.Message.Should().StartWith("Invalid role");
    }

    [Fact]
    public async Task Register_WithExistingEmail_ShouldReturnValidationErrorAndWriteNothing()
    {
        _users.Setup(u => u.ExistsAsync("a@test.com")).ReturnsAsync(true);

        var result = await _sut.RegisterAsync(new RegisterRequest("a@test.com", "Password123!", "Customer"));

        result.Error!.Kind.Should().Be(ErrorKind.Validation);
        result.Error.Message.Should().Be("Email already registered");
        _users.Verify(u => u.CreateAsync(It.IsAny<User>()), Times.Never);
        _companies.Verify(c => c.CreateAsync(It.IsAny<Company>()), Times.Never);
    }

    [Theory]
    [InlineData("customer")]
    [InlineData("CUSTOMER")]
    public async Task Register_Customer_ShouldStoreHashedPasswordAndNotCreateACompany(string role)
    {
        var result = await _sut.RegisterAsync(new RegisterRequest("c@test.com", "Password123!", role));

        result.IsSuccess.Should().BeTrue();
        result.Value.Role.Should().Be("Customer");
        result.Value.Email.Should().Be("c@test.com");
        result.Value.Token.Should().Be("token");
        _users.Verify(u => u.CreateAsync(It.Is<User>(x =>
            x.Email == "c@test.com" && x.PasswordHash == "hash:Password123!" && x.Role == UserRole.Customer)));
        _companies.Verify(c => c.CreateAsync(It.IsAny<Company>()), Times.Never);
        _users.Verify(u => u.UpdateAsync(It.IsAny<User>()), Times.Never);
        _jwt.Verify(j => j.GenerateToken(result.Value.UserId, "c@test.com", "Customer", null));
    }

    [Fact]
    public async Task Register_Owner_ShouldCreateDefaultCompanyLinkItAndPutItInTheToken()
    {
        var result = await _sut.RegisterAsync(new RegisterRequest("o@test.com", "Password123!", "Owner"));

        result.IsSuccess.Should().BeTrue();
        result.Value.Role.Should().Be("Owner");
        Company? created = null;
        _companies.Verify(c => c.CreateAsync(It.Is<Company>(x => Capture(x, out created))), Times.Once);
        created!.Name.Should().Be("o@test.com's Company");
        created.TimeZone.Should().Be("UTC");
        created.OwnerId.Should().Be(result.Value.UserId);
        _users.Verify(u => u.UpdateAsync(It.Is<User>(x => x.Id == result.Value.UserId && x.CompanyId == created.Id)), Times.Once);
        _jwt.Verify(j => j.GenerateToken(result.Value.UserId, "o@test.com", "Owner", created.Id), Times.Once);
    }

    [Fact]
    public async Task Register_ShouldNormalizeTheEmailForTheUniquenessCheckAndStorage()
    {
        _users.Setup(u => u.ExistsAsync("mixed@test.com")).ReturnsAsync(true);

        var duplicate = await _sut.RegisterAsync(new RegisterRequest("  Mixed@Test.com ", "Password123!", "Customer"));

        duplicate.Error!.Message.Should().Be("Email already registered");

        var created = await _sut.RegisterAsync(new RegisterRequest(" New@Test.COM", "Password123!", "Owner"));

        created.Value.Email.Should().Be("new@test.com");
        _users.Verify(u => u.CreateAsync(It.Is<User>(x => x.Email == "new@test.com")), Times.Once);
        _companies.Verify(c => c.CreateAsync(It.Is<Company>(x => x.Name == "new@test.com's Company")), Times.Once);
    }

    [Fact]
    public async Task Register_Owner_ShouldWriteUserAndCompanyInsideOneTransaction()
    {
        var insideTransaction = false;
        var writesInside = new List<string>();
        _unitOfWork.Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<Task>>(), It.IsAny<Func<Task<bool>>>()))
            .Returns(async (Func<Task> work, Func<Task<bool>> _) =>
            {
                insideTransaction = true;
                await work();
                insideTransaction = false;
            });
        _users.Setup(u => u.CreateAsync(It.IsAny<User>()))
            .ReturnsAsync((User u) => { writesInside.Add(insideTransaction ? "user" : "user-outside"); return u; });
        _companies.Setup(c => c.CreateAsync(It.IsAny<Company>()))
            .ReturnsAsync((Company c) => { writesInside.Add(insideTransaction ? "company" : "company-outside"); return c; });
        _users.Setup(u => u.UpdateAsync(It.IsAny<User>()))
            .ReturnsAsync((User u) => { writesInside.Add(insideTransaction ? "link" : "link-outside"); return u; });

        await _sut.RegisterAsync(new RegisterRequest("o@test.com", "Password123!", "Owner"));

        writesInside.Should().Equal("user", "company", "link");
        _unitOfWork.Verify(u => u.ExecuteInTransactionAsync(It.IsAny<Func<Task>>(), It.IsAny<Func<Task<bool>>>()), Times.Once);
    }

    [Fact]
    public async Task Register_Owner_ShouldPassAVerifyCallbackThatChecksTheFixedUserId()
    {
        Func<Task<bool>>? verify = null;
        _unitOfWork.Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<Task>>(), It.IsAny<Func<Task<bool>>>()))
            .Returns((Func<Task> work, Func<Task<bool>> v) => { verify = v; return work(); });

        var result = await _sut.RegisterAsync(new RegisterRequest("o@test.com", "Password123!", "Owner"));

        verify.Should().NotBeNull();
        _users.Setup(u => u.GetByIdAsync(result.Value.UserId)).ReturnsAsync((User?)null);
        (await verify!()).Should().BeFalse();
        _users.Setup(u => u.GetByIdAsync(result.Value.UserId)).ReturnsAsync(new User { Id = result.Value.UserId });
        (await verify()).Should().BeTrue();
    }

    [Fact]
    public async Task Register_Owner_WhenTheTransactionIsRetried_ShouldReuseTheSameIds()
    {
        var usersCreated = new List<User>();
        var companiesCreated = new List<Company>();
        _users.Setup(u => u.CreateAsync(It.IsAny<User>()))
            .ReturnsAsync((User u) => { usersCreated.Add(u); return u; });
        _companies.Setup(c => c.CreateAsync(It.IsAny<Company>()))
            .ReturnsAsync((Company c) => { companiesCreated.Add(c); return c; });
        // Simulates an ambiguous failure: the first run "commits" but the connection drops, the retry runs again.
        _unitOfWork.Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<Task>>(), It.IsAny<Func<Task<bool>>>()))
            .Returns(async (Func<Task> work, Func<Task<bool>> _) =>
            {
                await work();
                await work();
            });

        var result = await _sut.RegisterAsync(new RegisterRequest("o@test.com", "Password123!", "Owner"));

        usersCreated.Should().HaveCount(2);
        usersCreated.Select(u => u.Id).Distinct().Should().ContainSingle().Which.Should().NotBe(Guid.Empty);
        companiesCreated.Select(c => c.Id).Distinct().Should().ContainSingle().Which.Should().NotBe(Guid.Empty);
        usersCreated.Should().OnlyHaveUniqueItems(); // fresh entities per attempt, same ids
        result.Value.UserId.Should().Be(usersCreated[0].Id);
        _jwt.Verify(j => j.GenerateToken(result.Value.UserId, "o@test.com", "Owner", companiesCreated[0].Id), Times.Once);
    }

    [Fact]
    public async Task Register_Owner_WhenTheCompanyWriteFails_ShouldPropagateAndNotIssueAToken()
    {
        _companies.Setup(c => c.CreateAsync(It.IsAny<Company>())).ThrowsAsync(new InvalidOperationException("db down"));

        var act = () => _sut.RegisterAsync(new RegisterRequest("o@test.com", "Password123!", "Owner"));

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("db down");
        _users.Verify(u => u.UpdateAsync(It.IsAny<User>()), Times.Never);
        _jwt.Verify(j => j.GenerateToken(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid?>()), Times.Never);
    }

    [Fact]
    public async Task Register_WhenTheEmailIsTaken_ShouldNotOpenATransaction()
    {
        _users.Setup(u => u.ExistsAsync("o@test.com")).ReturnsAsync(true);

        await _sut.RegisterAsync(new RegisterRequest("o@test.com", "Password123!", "Owner"));

        _unitOfWork.Verify(u => u.ExecuteInTransactionAsync(It.IsAny<Func<Task>>(), It.IsAny<Func<Task<bool>>>()), Times.Never);
    }

    private static bool Capture(Company company, out Company? captured)
    {
        captured = company;
        return true;
    }

    // ---------- LoginAsync ----------

    [Fact]
    public async Task Login_WithValidCredentials_ShouldReturnTokenForTheUser()
    {
        var user = new User
        {
            Id = Guid.NewGuid(), Email = "o@test.com", PasswordHash = "hash:Password123!",
            Role = UserRole.Owner, CompanyId = Guid.NewGuid()
        };
        _users.Setup(u => u.GetByEmailAsync("o@test.com")).ReturnsAsync(user);

        var result = await _sut.LoginAsync(new LoginRequest("o@test.com", "Password123!"));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(new TokenResponse("token", user.Id, "o@test.com", "Owner"));
        _jwt.Verify(j => j.GenerateToken(user.Id, "o@test.com", "Owner", user.CompanyId), Times.Once);
    }

    [Fact]
    public async Task Login_WithUnknownEmail_ShouldReturnUnauthorized()
    {
        var result = await _sut.LoginAsync(new LoginRequest("ghost@test.com", "Password123!"));

        result.Error!.Kind.Should().Be(ErrorKind.Unauthorized);
        result.Error.Message.Should().Be("Invalid email or password");
        _jwt.Verify(j => j.GenerateToken(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Guid?>()), Times.Never);
    }

    [Fact]
    public async Task Login_ShouldLookTheNormalizedEmailUp()
    {
        var user = new User { Id = Guid.NewGuid(), Email = "o@test.com", PasswordHash = "hash:Password123!", Role = UserRole.Customer };
        _users.Setup(u => u.GetByEmailAsync("o@test.com")).ReturnsAsync(user);

        var result = await _sut.LoginAsync(new LoginRequest(" O@Test.COM ", "Password123!"));

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Login_WithUnknownEmail_ShouldStillVerifyAPasswordSoBothPathsCostTheSame()
    {
        await _sut.LoginAsync(new LoginRequest("ghost@test.com", "Password123!"));

        _hasher.Verify(h => h.Verify("Password123!", It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task Login_WithKnownEmail_ShouldVerifyAgainstTheStoredHashExactlyOnce()
    {
        _users.Setup(u => u.GetByEmailAsync("c@test.com"))
            .ReturnsAsync(new User { Id = Guid.NewGuid(), Email = "c@test.com", PasswordHash = "hash:Right1!", Role = UserRole.Customer });

        await _sut.LoginAsync(new LoginRequest("c@test.com", "Wrong1!"));

        _hasher.Verify(h => h.Verify("Wrong1!", "hash:Right1!"), Times.Once);
        _hasher.Verify(h => h.Verify(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task Login_WithWrongPassword_ShouldReturnTheSameErrorAsAnUnknownEmail()
    {
        _users.Setup(u => u.GetByEmailAsync("c@test.com"))
            .ReturnsAsync(new User { Id = Guid.NewGuid(), Email = "c@test.com", PasswordHash = "hash:Right1!", Role = UserRole.Customer });

        var wrongPassword = await _sut.LoginAsync(new LoginRequest("c@test.com", "Wrong1!"));
        var unknownEmail = await _sut.LoginAsync(new LoginRequest("ghost@test.com", "Wrong1!"));

        wrongPassword.Error.Should().Be(unknownEmail.Error);
        wrongPassword.Error!.Kind.Should().Be(ErrorKind.Unauthorized);
    }
}
