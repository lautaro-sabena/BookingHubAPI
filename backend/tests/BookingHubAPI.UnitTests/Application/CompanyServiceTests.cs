using BookingHubAPI.Application.Common;
using BookingHubAPI.Application.DTOs;
using BookingHubAPI.Application.Services;
using BookingHubAPI.Domain.Entities;
using BookingHubAPI.Domain.Interfaces;
using FluentAssertions;
using Moq;

namespace BookingHubAPI.UnitTests.Application;

public class CompanyServiceTests
{
    private readonly Mock<ICompanyRepository> _companies = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly CompanyService _sut;

    private readonly User _owner = new() { Id = Guid.NewGuid(), Role = UserRole.Owner };
    private readonly User _customer = new() { Id = Guid.NewGuid(), Role = UserRole.Customer };
    private readonly Company _company;

    public CompanyServiceTests()
    {
        _company = new Company
        {
            Id = Guid.NewGuid(), Name = "Acme", Description = "Desc", TimeZone = "UTC", IsActive = true, OwnerId = _owner.Id
        };

        _users.Setup(u => u.GetByIdAsync(_owner.Id)).ReturnsAsync(_owner);
        _users.Setup(u => u.GetByIdAsync(_customer.Id)).ReturnsAsync(_customer);
        _users.Setup(u => u.UpdateAsync(It.IsAny<User>())).ReturnsAsync((User u) => u);
        _companies.Setup(c => c.UpdateAsync(It.IsAny<Company>())).ReturnsAsync((Company c) => c);
        _companies.Setup(c => c.CreateAsync(It.IsAny<Company>()))
            .ReturnsAsync((Company c) => { c.Id = Guid.NewGuid(); return c; });

        _sut = new CompanyService(_companies.Object, _users.Object);
    }

    // ---------- GetMyCompanyAsync ----------

    [Fact]
    public async Task GetMyCompany_ForUnknownUser_ShouldReturnNotFound()
    {
        var result = await _sut.GetMyCompanyAsync(Guid.NewGuid());

        result.Error!.Kind.Should().Be(ErrorKind.NotFound);
    }

    [Fact]
    public async Task GetMyCompany_AsCustomer_ShouldReturnForbidden()
    {
        var result = await _sut.GetMyCompanyAsync(_customer.Id);

        result.Error!.Kind.Should().Be(ErrorKind.Forbidden);
        _companies.Verify(c => c.GetByOwnerIdAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task GetMyCompany_AsOwnerWithoutCompany_ShouldReturnNotFound()
    {
        var result = await _sut.GetMyCompanyAsync(_owner.Id);

        result.Error!.Kind.Should().Be(ErrorKind.NotFound);
    }

    [Fact]
    public async Task GetMyCompany_AsOwner_ShouldReturnTheOwnedCompany()
    {
        _companies.Setup(c => c.GetByOwnerIdAsync(_owner.Id)).ReturnsAsync(_company);

        var result = await _sut.GetMyCompanyAsync(_owner.Id);

        result.Value.Should().Be(new CompanyResponse(_company.Id, "Acme", "Desc", "UTC", true, _owner.Id));
    }

    // ---------- CreateCompanyAsync ----------

    [Fact]
    public async Task CreateCompany_ForUnknownUser_ShouldReturnNotFound()
    {
        var result = await _sut.CreateCompanyAsync(Guid.NewGuid(), new CompanyRequest("New", null, "UTC"));

        result.Error!.Kind.Should().Be(ErrorKind.NotFound);
    }

    [Fact]
    public async Task CreateCompany_AsCustomer_ShouldReturnForbiddenAndWriteNothing()
    {
        var result = await _sut.CreateCompanyAsync(_customer.Id, new CompanyRequest("New", null, "UTC"));

        result.Error!.Kind.Should().Be(ErrorKind.Forbidden);
        _companies.Verify(c => c.CreateAsync(It.IsAny<Company>()), Times.Never);
    }

    [Fact]
    public async Task CreateCompany_WhenOwnerAlreadyHasOne_ShouldReturnValidationError()
    {
        _companies.Setup(c => c.GetByOwnerIdAsync(_owner.Id)).ReturnsAsync(_company);

        var result = await _sut.CreateCompanyAsync(_owner.Id, new CompanyRequest("New", null, "UTC"));

        result.Error!.Kind.Should().Be(ErrorKind.Validation);
        result.Error.Message.Should().Be("You already have a company");
        _companies.Verify(c => c.CreateAsync(It.IsAny<Company>()), Times.Never);
    }

    [Fact]
    public async Task CreateCompany_AsOwnerWithoutCompany_ShouldCreateItAndLinkTheOwner()
    {
        var result = await _sut.CreateCompanyAsync(_owner.Id, new CompanyRequest("New", "About", "Europe/Madrid"));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(new CompanyResponse(result.Value.Id, "New", "About", "Europe/Madrid", true, _owner.Id));
        _owner.CompanyId.Should().Be(result.Value.Id);
        _users.Verify(u => u.UpdateAsync(_owner), Times.Once);
    }

    [Theory]
    [InlineData("Not/AZone")]
    [InlineData("Mars")]
    [InlineData("  ")]
    public async Task CreateCompany_WithUnknownTimeZone_ShouldReturnValidationErrorAndWriteNothing(string timeZone)
    {
        var result = await _sut.CreateCompanyAsync(_owner.Id, new CompanyRequest("New", null, timeZone));

        result.Error!.Kind.Should().Be(ErrorKind.Validation);
        result.Error.Message.Should().Be("Invalid time zone");
        _companies.Verify(c => c.CreateAsync(It.IsAny<Company>()), Times.Never);
    }

    [Theory]
    [InlineData("UTC")]
    [InlineData("America/Argentina/Buenos_Aires")]
    [InlineData("Europe/Madrid")]
    public async Task CreateCompany_WithKnownTimeZone_ShouldSucceed(string timeZone)
    {
        var result = await _sut.CreateCompanyAsync(_owner.Id, new CompanyRequest("New", null, timeZone));

        result.Value.TimeZone.Should().Be(timeZone);
    }

    [Fact]
    public async Task UpdateMyCompany_WithUnknownTimeZone_ShouldReturnValidationErrorAndChangeNothing()
    {
        _companies.Setup(c => c.GetByOwnerIdAsync(_owner.Id)).ReturnsAsync(_company);

        var result = await _sut.UpdateMyCompanyAsync(_owner.Id, new CompanyUpdateRequest("Renamed", null, "Not/AZone"));

        result.Error!.Kind.Should().Be(ErrorKind.Validation);
        _company.Name.Should().Be("Acme");
        _companies.Verify(c => c.UpdateAsync(It.IsAny<Company>()), Times.Never);
    }

    // ---------- UpdateMyCompanyAsync ----------

    [Fact]
    public async Task UpdateMyCompany_WithoutCompany_ShouldReturnNotFound()
    {
        var result = await _sut.UpdateMyCompanyAsync(_customer.Id, new CompanyUpdateRequest("X", null, null));

        result.Error!.Kind.Should().Be(ErrorKind.NotFound);
        _companies.Verify(c => c.UpdateAsync(It.IsAny<Company>()), Times.Never);
    }

    [Fact]
    public async Task UpdateMyCompany_ShouldLookTheCompanyUpByTheCallersOwnId()
    {
        _companies.Setup(c => c.GetByOwnerIdAsync(_owner.Id)).ReturnsAsync(_company);

        await _sut.UpdateMyCompanyAsync(_owner.Id, new CompanyUpdateRequest("Renamed", null, null));

        _companies.Verify(c => c.GetByOwnerIdAsync(_owner.Id), Times.Once);
        _companies.Verify(c => c.GetByOwnerIdAsync(It.Is<Guid>(id => id != _owner.Id)), Times.Never);
    }

    [Fact]
    public async Task UpdateMyCompany_ShouldApplyOnlyNonEmptyFieldsButAllowClearingTheDescription()
    {
        _companies.Setup(c => c.GetByOwnerIdAsync(_owner.Id)).ReturnsAsync(_company);

        var result = await _sut.UpdateMyCompanyAsync(_owner.Id, new CompanyUpdateRequest("", "", ""));

        result.Value.Should().Be(new CompanyResponse(_company.Id, "Acme", "", "UTC", true, _owner.Id));
    }

    [Fact]
    public async Task UpdateMyCompany_WithAllFields_ShouldApplyThem()
    {
        _companies.Setup(c => c.GetByOwnerIdAsync(_owner.Id)).ReturnsAsync(_company);

        var result = await _sut.UpdateMyCompanyAsync(_owner.Id, new CompanyUpdateRequest("Renamed", "New", "America/Lima"));

        result.Value.Should().Be(new CompanyResponse(_company.Id, "Renamed", "New", "America/Lima", true, _owner.Id));
    }

    [Fact]
    public async Task UpdateMyCompany_WithNullFields_ShouldChangeNothing()
    {
        _companies.Setup(c => c.GetByOwnerIdAsync(_owner.Id)).ReturnsAsync(_company);

        var result = await _sut.UpdateMyCompanyAsync(_owner.Id, new CompanyUpdateRequest(null, null, null));

        result.Value.Should().Be(new CompanyResponse(_company.Id, "Acme", "Desc", "UTC", true, _owner.Id));
    }
}
