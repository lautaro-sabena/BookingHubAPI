using BookingHubAPI.Application.DTOs;
using BookingHubAPI.Domain.Entities;
using FluentAssertions;
using Xunit;

namespace BookingHubAPI.UnitTests.Application;

public class ServiceDtoTests
{
    [Fact]
    public void ServiceResponse_ShouldContainCompanyInfo()
    {
        var serviceId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        var companyName = "Test Company";
        var companyDescription = "Test Company Description";

        var response = new ServiceResponse(
            serviceId,
            "Test Service",
            "Test Description",
            60,
            100.00m,
            true,
            companyId,
            companyName,
            companyDescription);

        response.Id.Should().Be(serviceId);
        response.Name.Should().Be("Test Service");
        response.CompanyId.Should().Be(companyId);
        response.CompanyName.Should().Be(companyName);
        response.CompanyDescription.Should().Be(companyDescription);
        response.IsActive.Should().BeTrue();
    }

    [Fact]
    public void ServiceResponse_ShouldAllowNullCompanyDescription()
    {
        var response = new ServiceResponse(
            Guid.NewGuid(),
            "Test Service",
            null,
            60,
            100.00m,
            true,
            Guid.NewGuid(),
            "Company",
            null);

        response.Description.Should().BeNull();
        response.CompanyDescription.Should().BeNull();
    }
}

public class ReservationDtoTests
{
    [Fact]
    public void ReservationResponse_ShouldContainCustomerEmail()
    {
        var reservationId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var customerEmail = "customer@test.com";
        var serviceId = Guid.NewGuid();

        var response = new ReservationResponse(
            reservationId,
            customerId,
            customerEmail,
            serviceId,
            "Test Service",
            60,
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(1).AddHours(1),
            "Pending",
            "Test notes",
            DateTime.UtcNow);

        response.Id.Should().Be(reservationId);
        response.CustomerId.Should().Be(customerId);
        response.CustomerEmail.Should().Be(customerEmail);
        response.ServiceName.Should().Be("Test Service");
        response.Status.Should().Be("Pending");
        response.Notes.Should().Be("Test notes");
    }

    [Fact]
    public void ReservationResponse_ShouldAllowNullNotes()
    {
        var response = new ReservationResponse(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "customer@test.com",
            Guid.NewGuid(),
            "Test Service",
            60,
            DateTime.UtcNow.AddDays(1),
            DateTime.UtcNow.AddDays(1).AddHours(1),
            "Pending",
            null,
            DateTime.UtcNow);

        response.Notes.Should().BeNull();
    }
}

public class WorkingHoursTests
{
    [Fact]
    public void WorkingHours_ShouldHaveCorrectDayOfWeek()
    {
        var workingHours = new WorkingHours
        {
            Id = Guid.NewGuid(),
            CompanyId = Guid.NewGuid(),
            DayOfWeek = DayOfWeek.Monday,
            StartTime = new TimeSpan(9, 0, 0),
            EndTime = new TimeSpan(17, 0, 0),
            IsActive = true
        };

        workingHours.DayOfWeek.Should().Be(DayOfWeek.Monday);
        workingHours.StartTime.Should().Be(new TimeSpan(9, 0, 0));
        workingHours.EndTime.Should().Be(new TimeSpan(17, 0, 0));
        workingHours.IsActive.Should().BeTrue();
    }

    [Theory]
    [InlineData(DayOfWeek.Sunday)]
    [InlineData(DayOfWeek.Monday)]
    [InlineData(DayOfWeek.Tuesday)]
    [InlineData(DayOfWeek.Wednesday)]
    [InlineData(DayOfWeek.Thursday)]
    [InlineData(DayOfWeek.Friday)]
    [InlineData(DayOfWeek.Saturday)]
    public void WorkingHours_ShouldSupportAllDaysOfWeek(DayOfWeek day)
    {
        var workingHours = new WorkingHours
        {
            Id = Guid.NewGuid(),
            CompanyId = Guid.NewGuid(),
            DayOfWeek = day,
            StartTime = new TimeSpan(9, 0, 0),
            EndTime = new TimeSpan(17, 0, 0),
            IsActive = true
        };

        workingHours.DayOfWeek.Should().Be(day);
    }
}
