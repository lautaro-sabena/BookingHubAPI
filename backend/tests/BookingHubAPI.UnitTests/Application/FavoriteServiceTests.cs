using BookingHubAPI.Application.Common;
using BookingHubAPI.Application.Services;
using BookingHubAPI.Domain.Entities;
using BookingHubAPI.Domain.Interfaces;
using FluentAssertions;
using Moq;
using ServiceEntity = BookingHubAPI.Domain.Entities.Service;

namespace BookingHubAPI.UnitTests.Application;

public class FavoriteServiceTests
{
    private readonly Mock<IFavoriteRepository> _favorites = new();
    private readonly Mock<IServiceRepository> _services = new();
    private readonly FavoriteService _sut;

    private readonly Guid _customerId = Guid.NewGuid();
    private readonly Company _company = new() { Id = Guid.NewGuid(), Name = "Acme" };
    private readonly ServiceEntity _service;

    public FavoriteServiceTests()
    {
        _service = new ServiceEntity
        {
            Id = Guid.NewGuid(), CompanyId = _company.Id, Company = _company, Name = "Haircut",
            Description = "Short", DurationMinutes = 30, Price = 12.5m
        };
        _services.Setup(s => s.GetByIdWithCompanyAsync(_service.Id)).ReturnsAsync(_service);
        _favorites.Setup(f => f.AddAsync(It.IsAny<Favorite>()))
            .ReturnsAsync((Favorite f) => { f.Id = Guid.NewGuid(); return f; });

        _sut = new FavoriteService(_favorites.Object, _services.Object);
    }

    [Fact]
    public async Task GetFavorites_ShouldMapEveryFieldOfTheCallersFavorites()
    {
        var favorite = new Favorite { Id = Guid.NewGuid(), CustomerId = _customerId, ServiceId = _service.Id, Service = _service };
        _favorites.Setup(f => f.GetByCustomerIdAsync(_customerId)).ReturnsAsync(new[] { favorite });

        var result = await _sut.GetFavoritesAsync(_customerId);

        var dto = result.Should().ContainSingle().Subject;
        dto.Id.Should().Be(favorite.Id);
        dto.ServiceId.Should().Be(_service.Id);
        dto.ServiceName.Should().Be("Haircut");
        dto.ServiceDescription.Should().Be("Short");
        dto.DurationMinutes.Should().Be(30);
        dto.Price.Should().Be(12.5m);
        dto.CompanyId.Should().Be(_company.Id);
        dto.CompanyName.Should().Be("Acme");
    }

    [Fact]
    public async Task AddFavorite_ForUnknownService_ShouldReturnNotFound()
    {
        var result = await _sut.AddFavoriteAsync(_customerId, Guid.NewGuid());

        result.Error!.Kind.Should().Be(ErrorKind.NotFound);
        result.Error.Message.Should().Be("Service not found");
        _favorites.Verify(f => f.AddAsync(It.IsAny<Favorite>()), Times.Never);
    }

    [Fact]
    public async Task AddFavorite_WhenAlreadyFavorited_ShouldReturnValidationError()
    {
        _favorites.Setup(f => f.GetByCustomerAndServiceAsync(_customerId, _service.Id))
            .ReturnsAsync(new Favorite { CustomerId = _customerId, ServiceId = _service.Id });

        var result = await _sut.AddFavoriteAsync(_customerId, _service.Id);

        result.Error!.Kind.Should().Be(ErrorKind.Validation);
        result.Error.Message.Should().Be("Service already in favorites");
        _favorites.Verify(f => f.AddAsync(It.IsAny<Favorite>()), Times.Never);
    }

    [Fact]
    public async Task AddFavorite_ShouldStoreTheFavoriteForTheCallerAndReturnTheMappedDto()
    {
        var result = await _sut.AddFavoriteAsync(_customerId, _service.Id);

        _favorites.Verify(f => f.AddAsync(It.Is<Favorite>(x => x.CustomerId == _customerId && x.ServiceId == _service.Id)), Times.Once);
        var dto = result.Value;
        dto.Id.Should().NotBeEmpty();
        dto.ServiceId.Should().Be(_service.Id);
        dto.ServiceName.Should().Be("Haircut");
        dto.ServiceDescription.Should().Be("Short");
        dto.DurationMinutes.Should().Be(30);
        dto.Price.Should().Be(12.5m);
        dto.CompanyId.Should().Be(_company.Id);
        dto.CompanyName.Should().Be("Acme");
    }

    [Fact]
    public async Task RemoveFavorite_WhenRemoved_ShouldSucceed()
    {
        _favorites.Setup(f => f.RemoveAsync(_customerId, _service.Id)).ReturnsAsync(true);

        var result = await _sut.RemoveFavoriteAsync(_customerId, _service.Id);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task RemoveFavorite_WhenNothingRemoved_ShouldReturnNotFound()
    {
        var result = await _sut.RemoveFavoriteAsync(_customerId, _service.Id);

        result.Error!.Kind.Should().Be(ErrorKind.NotFound);
        result.Error.Message.Should().Be("Favorite not found");
    }

    [Fact]
    public async Task RemoveFavorite_ShouldScopeTheRemovalToTheCallersOwnId()
    {
        // The service has no ownership rule of its own: it only stays safe by always passing the
        // caller's id to the repository, which deletes by (customer, service). Persistence-level
        // ownership is covered by the favorites integration tests.
        var otherCustomer = Guid.NewGuid();
        _favorites.Setup(f => f.RemoveAsync(_customerId, _service.Id)).ReturnsAsync(true);

        var mine = await _sut.RemoveFavoriteAsync(_customerId, _service.Id);
        var theirs = await _sut.RemoveFavoriteAsync(otherCustomer, _service.Id);

        mine.IsSuccess.Should().BeTrue();
        theirs.Error!.Kind.Should().Be(ErrorKind.NotFound);
        _favorites.Verify(f => f.RemoveAsync(_customerId, _service.Id), Times.Once);
        _favorites.Verify(f => f.RemoveAsync(otherCustomer, _service.Id), Times.Once);
    }

    [Fact]
    public async Task AddFavorite_ForInactiveService_ShouldReturnNotFoundAndStoreNothing()
    {
        _service.IsActive = false;

        var result = await _sut.AddFavoriteAsync(_customerId, _service.Id);

        result.Error!.Kind.Should().Be(ErrorKind.NotFound);
        result.Error.Message.Should().Be("Service not found");
        _favorites.Verify(f => f.AddAsync(It.IsAny<Favorite>()), Times.Never);
    }

    [Fact]
    public async Task AddFavorite_ForServiceOfInactiveCompany_ShouldReturnNotFoundAndStoreNothing()
    {
        _company.IsActive = false;

        var result = await _sut.AddFavoriteAsync(_customerId, _service.Id);

        result.Error!.Kind.Should().Be(ErrorKind.NotFound);
        result.Error.Message.Should().Be("Service not found");
        _favorites.Verify(f => f.AddAsync(It.IsAny<Favorite>()), Times.Never);
    }

    [Fact]
    public async Task AddFavorite_ForHiddenService_ShouldNotRevealWhetherItWasAlreadyFavorited()
    {
        _service.IsActive = false;
        _favorites.Setup(f => f.GetByCustomerAndServiceAsync(_customerId, _service.Id))
            .ReturnsAsync(new Favorite { CustomerId = _customerId, ServiceId = _service.Id });

        var result = await _sut.AddFavoriteAsync(_customerId, _service.Id);

        result.Error!.Kind.Should().Be(ErrorKind.NotFound);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task IsFavorite_ShouldReflectTheRepository(bool exists)
    {
        _favorites.Setup(f => f.ExistsAsync(_customerId, _service.Id)).ReturnsAsync(exists);

        (await _sut.IsFavoriteAsync(_customerId, _service.Id)).Should().Be(exists);
    }
}
