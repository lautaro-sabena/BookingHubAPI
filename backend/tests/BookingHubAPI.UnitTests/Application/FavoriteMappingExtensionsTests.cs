using BookingHubAPI.Application.DTOs;
using BookingHubAPI.Domain.Entities;
using FluentAssertions;
using Xunit;

namespace BookingHubAPI.UnitTests.Application;

public class FavoriteMappingExtensionsTests
{
    [Fact]
    public void ToDto_ShouldMapEveryField()
    {
        var company = new Company { Id = Guid.NewGuid(), Name = "Acme Salon" };
        var service = new Service
        {
            Id = Guid.NewGuid(),
            Name = "Haircut",
            Description = "A quick trim",
            DurationMinutes = 45,
            Price = 25.50m,
            CompanyId = company.Id,
            Company = company
        };
        var favorite = new Favorite { Id = Guid.NewGuid(), ServiceId = service.Id, Service = service };

        var dto = favorite.ToDto();

        dto.Id.Should().Be(favorite.Id);
        dto.ServiceId.Should().Be(service.Id);
        dto.ServiceName.Should().Be("Haircut");
        dto.ServiceDescription.Should().Be("A quick trim");
        dto.DurationMinutes.Should().Be(45);
        dto.Price.Should().Be(25.50m);
        dto.CompanyId.Should().Be(company.Id);
        dto.CompanyName.Should().Be("Acme Salon");
    }

    [Fact]
    public void ToDto_ShouldKeepNullDescription()
    {
        var company = new Company { Id = Guid.NewGuid(), Name = "Acme Salon" };
        var service = new Service { Id = Guid.NewGuid(), Name = "Massage", Description = null, CompanyId = company.Id, Company = company };
        var favorite = new Favorite { Service = service };

        favorite.ToDto().ServiceDescription.Should().BeNull();
    }
}
