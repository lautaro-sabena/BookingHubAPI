using BookingHubAPI.Domain.Entities;
using FluentAssertions;
using Xunit;

namespace BookingHubAPI.UnitTests.Domain;

public class UserTests
{
    [Fact]
    public void User_ShouldHaveValidId()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@test.com",
            PasswordHash = "hash",
            Role = UserRole.Customer
        };

        user.Id.Should().NotBeEmpty();
        user.Email.Should().NotBeEmpty();
    }

    [Theory]
    [InlineData(UserRole.Owner)]
    [InlineData(UserRole.Customer)]
    public void User_ShouldHaveValidRole(UserRole role)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@test.com",
            PasswordHash = "hash",
            Role = role
        };

        user.Role.Should().Be(role);
    }

    [Fact]
    public void User_ShouldHaveDefaultTimestamps()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@test.com",
            PasswordHash = "hash",
            Role = UserRole.Customer
        };

        user.CreatedAt.Should().Be(default(DateTime));
    }
}

public class ReservationTests
{
    [Fact]
    public void Reservation_ShouldStartWithPendingStatus()
    {
        var reservation = new Reservation
        {
            Id = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            ServiceId = Guid.NewGuid(),
            CompanyId = Guid.NewGuid(),
            StartTime = DateTime.UtcNow.AddDays(1),
            EndTime = DateTime.UtcNow.AddDays(1).AddHours(1),
            Status = ReservationStatus.Pending
        };

        reservation.Status.Should().Be(ReservationStatus.Pending);
    }

    [Theory]
    [InlineData(ReservationStatus.Pending)]
    [InlineData(ReservationStatus.Confirmed)]
    [InlineData(ReservationStatus.Cancelled)]
    [InlineData(ReservationStatus.Completed)]
    public void Reservation_ShouldSupportAllStatuses(ReservationStatus status)
    {
        var reservation = new Reservation
        {
            Id = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            ServiceId = Guid.NewGuid(),
            CompanyId = Guid.NewGuid(),
            StartTime = DateTime.UtcNow.AddDays(1),
            EndTime = DateTime.UtcNow.AddDays(1).AddHours(1),
            Status = status
        };

        reservation.Status.Should().Be(status);
    }

    [Fact]
    public void Reservation_ShouldCalculateEndTime()
    {
        var startTime = DateTime.UtcNow.AddDays(1).Date.AddHours(10);
        var durationMinutes = 60;
        
        var reservation = new Reservation
        {
            Id = Guid.NewGuid(),
            CustomerId = Guid.NewGuid(),
            ServiceId = Guid.NewGuid(),
            CompanyId = Guid.NewGuid(),
            StartTime = startTime,
            EndTime = startTime.AddMinutes(durationMinutes),
            Status = ReservationStatus.Pending
        };

        reservation.EndTime.Should().Be(startTime.AddMinutes(durationMinutes));
    }
}

public class ServiceTests
{
    [Fact]
    public void Service_ShouldBeActiveByDefault()
    {
        var service = new BookingHubAPI.Domain.Entities.Service
        {
            Id = Guid.NewGuid(),
            CompanyId = Guid.NewGuid(),
            Name = "Test Service",
            DurationMinutes = 30,
            Price = 100
        };

        service.IsActive.Should().BeTrue();
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(-1, false)]
    [InlineData(30, true)]
    [InlineData(60, true)]
    public void Service_ShouldValidateDuration(int duration, bool isValid)
    {
        var service = new BookingHubAPI.Domain.Entities.Service
        {
            Id = Guid.NewGuid(),
            CompanyId = Guid.NewGuid(),
            Name = "Test Service",
            DurationMinutes = duration,
            Price = 100
        };

        if (isValid)
        {
            service.DurationMinutes.Should().BeGreaterThan(0);
        }
        else
        {
            service.DurationMinutes.Should().BeLessThanOrEqualTo(0);
        }
    }

    [Theory]
    [InlineData(-10, false)]
    [InlineData(0, false)]
    [InlineData(50.50, true)]
    [InlineData(100.00, true)]
    public void Service_ShouldValidatePrice(decimal price, bool isValid)
    {
        var service = new BookingHubAPI.Domain.Entities.Service
        {
            Id = Guid.NewGuid(),
            CompanyId = Guid.NewGuid(),
            Name = "Test Service",
            DurationMinutes = 30,
            Price = price
        };

        if (isValid)
        {
            service.Price.Should().BeGreaterThanOrEqualTo(0);
        }
    }
}
