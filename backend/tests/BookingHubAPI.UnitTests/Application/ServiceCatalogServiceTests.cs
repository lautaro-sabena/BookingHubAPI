using BookingHubAPI.Application.Common;
using BookingHubAPI.Application.DTOs;
using BookingHubAPI.Application.Services;
using BookingHubAPI.Domain.Entities;
using BookingHubAPI.Domain.Interfaces;
using FluentAssertions;
using Moq;
using ServiceEntity = BookingHubAPI.Domain.Entities.Service;

namespace BookingHubAPI.UnitTests.Application;

public class ServiceCatalogServiceTests
{
    private readonly Mock<IServiceRepository> _services = new();
    private readonly Mock<ICompanyRepository> _companies = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly ServiceCatalogService _sut;

    private readonly Company _company = new() { Id = Guid.NewGuid(), Name = "Acme", Description = "Acme desc", IsActive = true };
    private readonly Company _otherCompany = new() { Id = Guid.NewGuid(), Name = "Other", IsActive = true };
    private readonly User _customer;
    private readonly User _owner;
    private readonly User _otherOwner;
    private readonly User _ownerWithoutCompany;
    private readonly ServiceEntity _service;
    private readonly ServiceEntity _otherService;

    public ServiceCatalogServiceTests()
    {
        _customer = new User { Id = Guid.NewGuid(), Role = UserRole.Customer };
        _owner = new User { Id = Guid.NewGuid(), Role = UserRole.Owner, CompanyId = _company.Id };
        _otherOwner = new User { Id = Guid.NewGuid(), Role = UserRole.Owner, CompanyId = _otherCompany.Id };
        _ownerWithoutCompany = new User { Id = Guid.NewGuid(), Role = UserRole.Owner };
        _service = NewService(_company, "Haircut");
        _otherService = NewService(_otherCompany, "Massage");

        foreach (var user in new[] { _customer, _owner, _otherOwner, _ownerWithoutCompany })
        {
            _users.Setup(u => u.GetByIdAsync(user.Id)).ReturnsAsync(user);
        }

        foreach (var service in new[] { _service, _otherService })
        {
            _services.Setup(s => s.GetByIdAsync(service.Id)).ReturnsAsync(service);
            _services.Setup(s => s.GetByIdWithCompanyAsync(service.Id)).ReturnsAsync(service);
        }

        _services.Setup(s => s.UpdateAsync(It.IsAny<ServiceEntity>())).ReturnsAsync((ServiceEntity s) => s);
        _services.Setup(s => s.CreateAsync(It.IsAny<ServiceEntity>()))
            .ReturnsAsync((ServiceEntity s) => { s.Id = Guid.NewGuid(); return s; });
        _companies.Setup(c => c.GetByIdAsync(_company.Id)).ReturnsAsync(_company);

        _sut = new ServiceCatalogService(_services.Object, _companies.Object, _users.Object);
    }

    private static ServiceEntity NewService(Company company, string name) => new()
    {
        Id = Guid.NewGuid(), CompanyId = company.Id, Company = company, Name = name,
        Description = "desc", DurationMinutes = 30, Price = 10m, IsActive = true
    };

    // ---------- GetOwnServicesAsync ----------

    [Fact]
    public async Task GetOwnServices_ForUnknownUser_ShouldReturnNotFound()
    {
        var result = await _sut.GetOwnServicesAsync(Guid.NewGuid(), 1, 10, null);

        result.Error!.Kind.Should().Be(ErrorKind.NotFound);
    }

    [Fact]
    public async Task GetOwnServices_ForCustomerOrOwnerWithoutCompany_ShouldReturnForbidden()
    {
        var asCustomer = await _sut.GetOwnServicesAsync(_customer.Id, 1, 10, null);
        var withoutCompany = await _sut.GetOwnServicesAsync(_ownerWithoutCompany.Id, 1, 10, null);

        asCustomer.Error!.Kind.Should().Be(ErrorKind.Forbidden);
        withoutCompany.Error!.Kind.Should().Be(ErrorKind.Forbidden);
    }

    [Fact]
    public async Task GetOwnServices_ShouldQueryOnlyTheOwnersCompanyAndComputeTotalPages()
    {
        _services.Setup(s => s.GetByCompanyIdAsync(_company.Id, 2, 2, "Hair")).ReturnsAsync(new[] { _service });
        _services.Setup(s => s.GetCountByCompanyIdAsync(_company.Id, "Hair")).ReturnsAsync(5);

        var result = await _sut.GetOwnServicesAsync(_owner.Id, 2, 2, "Hair");

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCount.Should().Be(5);
        result.Value.TotalPages.Should().Be(3);
        result.Value.Page.Should().Be(2);
        result.Value.PageSize.Should().Be(2);
        var item = result.Value.Items.Single();
        item.Id.Should().Be(_service.Id);
        item.CompanyName.Should().Be("Acme");
        item.CompanyDescription.Should().Be("Acme desc");
        _services.Verify(s => s.GetByCompanyIdAsync(_otherCompany.Id, It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string?>()), Times.Never);
    }

    [Fact]
    public async Task GetOwnServices_WithNoServices_ShouldReturnZeroPages()
    {
        _services.Setup(s => s.GetByCompanyIdAsync(_company.Id, 1, 10, null)).ReturnsAsync(Array.Empty<ServiceEntity>());
        _services.Setup(s => s.GetCountByCompanyIdAsync(_company.Id, null)).ReturnsAsync(0);

        var result = await _sut.GetOwnServicesAsync(_owner.Id, 1, 10, null);

        result.Value.Items.Should().BeEmpty();
        result.Value.TotalPages.Should().Be(0);
    }

    // ---------- GetPublicServicesAsync ----------

    [Fact]
    public async Task GetPublicServices_ShouldMapServicesAndComputeTotalPages()
    {
        _services.Setup(s => s.GetAllActiveAsync(1, 4)).ReturnsAsync(new[] { _service, _otherService });
        _services.Setup(s => s.GetAllActiveCountAsync()).ReturnsAsync(9);

        var page = await _sut.GetPublicServicesAsync(1, 4);

        page.Items.Select(i => i.CompanyName).Should().Equal("Acme", "Other");
        page.TotalCount.Should().Be(9);
        page.TotalPages.Should().Be(3);
    }

    // ---------- GetServiceAsync ----------

    [Fact]
    public async Task GetService_ForUnknownUserOrService_ShouldReturnNotFound()
    {
        var unknownUser = await _sut.GetServiceAsync(Guid.NewGuid(), _service.Id);
        var unknownService = await _sut.GetServiceAsync(_customer.Id, Guid.NewGuid());

        unknownUser.Error!.Kind.Should().Be(ErrorKind.NotFound);
        unknownService.Error!.Kind.Should().Be(ErrorKind.NotFound);
    }

    [Fact]
    public async Task GetService_ActiveServiceOfActiveCompany_ShouldBeVisibleToAnyone()
    {
        var asCustomer = await _sut.GetServiceAsync(_customer.Id, _service.Id);
        var asOtherOwner = await _sut.GetServiceAsync(_otherOwner.Id, _service.Id);

        asCustomer.Value.Id.Should().Be(_service.Id);
        asOtherOwner.Value.Id.Should().Be(_service.Id);
    }

    [Fact]
    public async Task GetService_InactiveService_ShouldBeVisibleOnlyToItsOwnCompanyOwner()
    {
        _service.IsActive = false;

        var own = await _sut.GetServiceAsync(_owner.Id, _service.Id);
        var otherOwner = await _sut.GetServiceAsync(_otherOwner.Id, _service.Id);
        var customer = await _sut.GetServiceAsync(_customer.Id, _service.Id);

        own.Value.IsActive.Should().BeFalse();
        otherOwner.Error!.Kind.Should().Be(ErrorKind.Forbidden);
        customer.Error!.Kind.Should().Be(ErrorKind.Forbidden);
    }

    [Fact]
    public async Task GetService_OfInactiveCompany_ShouldBeVisibleOnlyToItsOwnCompanyOwner()
    {
        _company.IsActive = false;

        var own = await _sut.GetServiceAsync(_owner.Id, _service.Id);
        var customer = await _sut.GetServiceAsync(_customer.Id, _service.Id);

        own.IsSuccess.Should().BeTrue();
        customer.Error!.Kind.Should().Be(ErrorKind.Forbidden);
    }

    // ---------- CreateServiceAsync ----------

    [Fact]
    public async Task CreateService_ForCustomerUnknownUserOrOwnerWithoutCompany_ShouldReturnForbidden()
    {
        var request = new ServiceRequest("Cut", null, 30, 10m);

        (await _sut.CreateServiceAsync(_customer.Id, request)).Error!.Kind.Should().Be(ErrorKind.Forbidden);
        (await _sut.CreateServiceAsync(Guid.NewGuid(), request)).Error!.Kind.Should().Be(ErrorKind.Forbidden);
        (await _sut.CreateServiceAsync(_ownerWithoutCompany.Id, request)).Error!.Kind.Should().Be(ErrorKind.Forbidden);
        _services.Verify(s => s.CreateAsync(It.IsAny<ServiceEntity>()), Times.Never);
    }

    [Fact]
    public async Task CreateService_ForOwner_ShouldCreateActiveServiceInTheOwnersCompany()
    {
        var result = await _sut.CreateServiceAsync(_owner.Id, new ServiceRequest("Cut", "Short", 45, 12.5m));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(new
        {
            Name = "Cut", Description = "Short", DurationMinutes = 45, Price = 12.5m,
            IsActive = true, CompanyId = _company.Id, CompanyName = "Acme", CompanyDescription = "Acme desc"
        });
        _services.Verify(s => s.CreateAsync(It.Is<ServiceEntity>(x => x.CompanyId == _company.Id)), Times.Once);
    }

    [Fact]
    public async Task CreateService_WhenCompanyLookupReturnsNull_ShouldUseAnEmptyCompanyName()
    {
        _companies.Setup(c => c.GetByIdAsync(_company.Id)).ReturnsAsync((Company?)null);

        var result = await _sut.CreateServiceAsync(_owner.Id, new ServiceRequest("Cut", null, 30, 1m));

        result.Value.CompanyName.Should().BeEmpty();
        result.Value.CompanyDescription.Should().BeNull();
    }

    // ---------- UpdateServiceAsync ----------

    [Fact]
    public async Task UpdateService_ForCustomerOrOwnerWithoutCompany_ShouldReturnForbidden()
    {
        var request = new ServiceUpdateRequest("X", null, null, null, null);

        (await _sut.UpdateServiceAsync(_customer.Id, _service.Id, request)).Error!.Kind.Should().Be(ErrorKind.Forbidden);
        (await _sut.UpdateServiceAsync(_ownerWithoutCompany.Id, _service.Id, request)).Error!.Kind.Should().Be(ErrorKind.Forbidden);
        _services.Verify(s => s.UpdateAsync(It.IsAny<ServiceEntity>()), Times.Never);
    }

    [Fact]
    public async Task UpdateService_OfAnotherCompany_ShouldReturnNotFoundAndNotSave()
    {
        var result = await _sut.UpdateServiceAsync(
            _otherOwner.Id, _service.Id, new ServiceUpdateRequest("Hacked", null, null, null, false));

        result.Error!.Kind.Should().Be(ErrorKind.NotFound);
        _service.Name.Should().Be("Haircut");
        _service.IsActive.Should().BeTrue();
        _services.Verify(s => s.UpdateAsync(It.IsAny<ServiceEntity>()), Times.Never);
    }

    [Fact]
    public async Task UpdateService_ForUnknownService_ShouldReturnNotFound()
    {
        var result = await _sut.UpdateServiceAsync(_owner.Id, Guid.NewGuid(), new ServiceUpdateRequest("X", null, null, null, null));

        result.Error!.Kind.Should().Be(ErrorKind.NotFound);
    }

    [Fact]
    public async Task UpdateService_ShouldApplyOnlyTheProvidedFields()
    {
        var result = await _sut.UpdateServiceAsync(
            _owner.Id, _service.Id, new ServiceUpdateRequest(null, "New desc", null, 99m, false));

        result.Value.Should().BeEquivalentTo(new
        {
            Name = "Haircut", Description = "New desc", DurationMinutes = 30, Price = 99m, IsActive = false,
            CompanyName = "Acme"
        });
    }

    [Fact]
    public async Task UpdateService_WithEmptyName_ShouldKeepTheNameButEmptyDescriptionClearsIt()
    {
        var result = await _sut.UpdateServiceAsync(
            _owner.Id, _service.Id, new ServiceUpdateRequest("", "", null, null, null));

        result.Value.Name.Should().Be("Haircut");
        result.Value.Description.Should().BeEmpty();
    }

    // ---------- DeleteServiceAsync ----------

    [Fact]
    public async Task DeleteService_ForCustomerOrOwnerWithoutCompany_ShouldReturnForbidden()
    {
        (await _sut.DeleteServiceAsync(_customer.Id, _service.Id)).Error!.Kind.Should().Be(ErrorKind.Forbidden);
        (await _sut.DeleteServiceAsync(_ownerWithoutCompany.Id, _service.Id)).Error!.Kind.Should().Be(ErrorKind.Forbidden);
        _service.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteService_OfAnotherCompany_ShouldReturnNotFoundAndKeepItActive()
    {
        var result = await _sut.DeleteServiceAsync(_otherOwner.Id, _service.Id);

        result.Error!.Kind.Should().Be(ErrorKind.NotFound);
        _service.IsActive.Should().BeTrue();
        _services.Verify(s => s.UpdateAsync(It.IsAny<ServiceEntity>()), Times.Never);
    }

    [Fact]
    public async Task DeleteService_OfOwnCompany_ShouldDeactivateIt()
    {
        var result = await _sut.DeleteServiceAsync(_owner.Id, _service.Id);

        result.IsSuccess.Should().BeTrue();
        _service.IsActive.Should().BeFalse();
        _services.Verify(s => s.UpdateAsync(_service), Times.Once);
    }
}
