using BookingHubAPI.Application.Common;
using BookingHubAPI.Application.DTOs;
using BookingHubAPI.Application.Services;
using BookingHubAPI.Domain.Entities;
using BookingHubAPI.Domain.Interfaces;
using FluentAssertions;
using Moq;

namespace BookingHubAPI.UnitTests.Application;

public class WorkingHoursServiceTests
{
    private readonly Mock<IWorkingHoursRepository> _hours = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly WorkingHoursService _sut;

    private readonly Guid _companyId = Guid.NewGuid();
    private readonly User _customer = new() { Id = Guid.NewGuid(), Role = UserRole.Customer };
    private readonly User _owner;
    private readonly User _ownerWithoutCompany = new() { Id = Guid.NewGuid(), Role = UserRole.Owner };

    public WorkingHoursServiceTests()
    {
        _owner = new User { Id = Guid.NewGuid(), Role = UserRole.Owner, CompanyId = _companyId };
        foreach (var user in new[] { _customer, _owner, _ownerWithoutCompany })
        {
            _users.Setup(u => u.GetByIdAsync(user.Id)).ReturnsAsync(user);
        }

        _hours.Setup(h => h.GetByCompanyIdAsync(_companyId)).ReturnsAsync(Array.Empty<WorkingHours>());
        _hours.Setup(h => h.CreateAsync(It.IsAny<WorkingHours>())).ReturnsAsync((WorkingHours w) => w);

        _sut = new WorkingHoursService(_hours.Object, _users.Object);
    }

    private static WorkingHours Stored(DayOfWeek day, int start, int end, bool isActive = true) => new()
    {
        DayOfWeek = day, StartTime = new TimeSpan(start, 0, 0), EndTime = new TimeSpan(end, 0, 0), IsActive = isActive
    };

    private static WorkingHoursRequest Request(DayOfWeek day, int start, int end, bool isActive = true) =>
        new(day, new TimeSpan(start, 0, 0), new TimeSpan(end, 0, 0), isActive);

    // ---------- authorization ----------

    [Fact]
    public async Task GetWorkingHours_ForCustomerUnknownUserOrOwnerWithoutCompany_ShouldReturnForbidden()
    {
        foreach (var userId in new[] { _customer.Id, Guid.NewGuid(), _ownerWithoutCompany.Id })
        {
            var result = await _sut.GetWorkingHoursAsync(userId);

            result.Error!.Kind.Should().Be(ErrorKind.Forbidden);
        }
    }

    [Fact]
    public async Task ReplaceWorkingHours_ForCustomerUnknownUserOrOwnerWithoutCompany_ShouldReturnForbiddenAndTouchNothing()
    {
        foreach (var userId in new[] { _customer.Id, Guid.NewGuid(), _ownerWithoutCompany.Id })
        {
            var result = await _sut.ReplaceWorkingHoursAsync(userId, new[] { Request(DayOfWeek.Monday, 8, 12) });

            result.Error!.Kind.Should().Be(ErrorKind.Forbidden);
        }

        _hours.Verify(h => h.DeleteByCompanyIdAsync(It.IsAny<Guid>()), Times.Never);
        _hours.Verify(h => h.CreateAsync(It.IsAny<WorkingHours>()), Times.Never);
    }

    // ---------- GetWorkingHoursAsync ----------

    [Fact]
    public async Task GetWorkingHours_WhenNothingIsConfigured_ShouldReturnSevenInactiveDaysWithDefaultWindow()
    {
        var result = await _sut.GetWorkingHoursAsync(_owner.Id);

        result.Value.Should().HaveCount(7);
        result.Value.Select(d => d.DayOfWeek).Should().Equal(Enum.GetValues<DayOfWeek>());
        result.Value.Should().OnlyContain(d =>
            !d.IsActive && d.StartTime == new TimeSpan(9, 0, 0) && d.EndTime == new TimeSpan(17, 0, 0));
    }

    [Fact]
    public async Task GetWorkingHours_ShouldOverlayConfiguredDaysOntoTheDefaults()
    {
        _hours.Setup(h => h.GetByCompanyIdAsync(_companyId))
            .ReturnsAsync(new[] { Stored(DayOfWeek.Monday, 8, 12), Stored(DayOfWeek.Saturday, 10, 14) });

        var result = await _sut.GetWorkingHoursAsync(_owner.Id);

        result.Value.Single(d => d.DayOfWeek == DayOfWeek.Monday).Should().Be(
            new WorkingHoursResponse(DayOfWeek.Monday, new TimeSpan(8, 0, 0), new TimeSpan(12, 0, 0), true));
        result.Value.Single(d => d.DayOfWeek == DayOfWeek.Saturday).StartTime.Should().Be(new TimeSpan(10, 0, 0));
        result.Value.Count(d => d.IsActive).Should().Be(2);
    }

    [Fact]
    public async Task GetWorkingHours_ForAStoredInactiveDay_ShouldKeepItsTimesButReportItInactive()
    {
        _hours.Setup(h => h.GetByCompanyIdAsync(_companyId))
            .ReturnsAsync(new[] { Stored(DayOfWeek.Tuesday, 7, 8, isActive: false) });

        var tuesday = (await _sut.GetWorkingHoursAsync(_owner.Id)).Value.Single(d => d.DayOfWeek == DayOfWeek.Tuesday);

        tuesday.IsActive.Should().BeFalse();
        tuesday.StartTime.Should().Be(new TimeSpan(7, 0, 0));
    }

    [Fact]
    public async Task GetWorkingHours_ShouldQueryOnlyTheOwnersCompany()
    {
        await _sut.GetWorkingHoursAsync(_owner.Id);

        _hours.Verify(h => h.GetByCompanyIdAsync(_companyId), Times.Once);
        _hours.Verify(h => h.GetByCompanyIdAsync(It.Is<Guid>(id => id != _companyId)), Times.Never);
    }

    // ---------- ReplaceWorkingHoursAsync ----------

    [Fact]
    public async Task ReplaceWorkingHours_ShouldDeleteThePreviousScheduleAndStoreEveryEntryIncludingInactiveOnes()
    {
        var created = new List<WorkingHours>();
        _hours.Setup(h => h.CreateAsync(It.IsAny<WorkingHours>()))
            .Callback((WorkingHours w) => created.Add(w))
            .ReturnsAsync((WorkingHours w) => w);

        var result = await _sut.ReplaceWorkingHoursAsync(_owner.Id, new[]
        {
            Request(DayOfWeek.Monday, 8, 12),
            Request(DayOfWeek.Tuesday, 6, 7, isActive: false),
            Request(DayOfWeek.Friday, 10, 20)
        });

        result.IsSuccess.Should().BeTrue();
        _hours.Verify(h => h.DeleteByCompanyIdAsync(_companyId), Times.Once);
        created.Select(w => w.DayOfWeek).Should().Equal(DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Friday);
        created.Should().OnlyContain(w => w.CompanyId == _companyId);
        created.Select(w => w.IsActive).Should().Equal(true, false, true);
        created[1].StartTime.Should().Be(new TimeSpan(6, 0, 0));
        created[1].EndTime.Should().Be(new TimeSpan(7, 0, 0));
        created[2].StartTime.Should().Be(new TimeSpan(10, 0, 0));
        created[2].EndTime.Should().Be(new TimeSpan(20, 0, 0));
    }

    [Fact]
    public async Task ReplaceWorkingHours_ShouldReturnTheSavedSevenDaySchedule()
    {
        var result = await _sut.ReplaceWorkingHoursAsync(_owner.Id, new[]
        {
            Request(DayOfWeek.Monday, 8, 12),
            Request(DayOfWeek.Tuesday, 6, 7, isActive: false)
        });

        result.Value.Should().HaveCount(7);
        result.Value.Select(d => d.DayOfWeek).Should().Equal(Enum.GetValues<DayOfWeek>());
        result.Value.Single(d => d.DayOfWeek == DayOfWeek.Monday).Should().Be(
            new WorkingHoursResponse(DayOfWeek.Monday, new TimeSpan(8, 0, 0), new TimeSpan(12, 0, 0), true));
        result.Value.Single(d => d.DayOfWeek == DayOfWeek.Tuesday).Should().Be(
            new WorkingHoursResponse(DayOfWeek.Tuesday, new TimeSpan(6, 0, 0), new TimeSpan(7, 0, 0), false));
        result.Value.Single(d => d.DayOfWeek == DayOfWeek.Sunday).Should().Be(
            new WorkingHoursResponse(DayOfWeek.Sunday, new TimeSpan(9, 0, 0), new TimeSpan(17, 0, 0), false));
        _hours.Verify(h => h.GetByCompanyIdAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Theory]
    [InlineData(-1, 8, true)]
    [InlineData(8, 25, true)]
    [InlineData(25, 26, true)]
    [InlineData(-1, 8, false)]   // an inactive day keeps its times, but they still have to be a real time of day
    [InlineData(8, 25, false)]
    public async Task ReplaceWorkingHours_WithTimesOutsideTheDay_ShouldReturnValidationAndTouchNothing(
        int start, int end, bool isActive)
    {
        var result = await _sut.ReplaceWorkingHoursAsync(
            _owner.Id, new[] { Request(DayOfWeek.Thursday, start, end, isActive) });

        result.Error!.Kind.Should().Be(ErrorKind.Validation);
        result.Error.Message.Should().Be("Working hours must be within 00:00 and 24:00 for Thursday");
        _hours.Verify(h => h.DeleteByCompanyIdAsync(It.IsAny<Guid>()), Times.Never);
        _hours.Verify(h => h.CreateAsync(It.IsAny<WorkingHours>()), Times.Never);
    }

    [Fact]
    public async Task ReplaceWorkingHours_WithADayRunningUntilMidnight_ShouldBeAccepted()
    {
        var result = await _sut.ReplaceWorkingHoursAsync(_owner.Id, new[] { Request(DayOfWeek.Friday, 0, 24) });

        result.IsSuccess.Should().BeTrue();
    }

    [Theory]
    [InlineData(12, 8)]
    [InlineData(8, 8)]
    public async Task ReplaceWorkingHours_WithActiveDayStartingNotBeforeItsEnd_ShouldReturnValidationAndTouchNothing(
        int start, int end)
    {
        var result = await _sut.ReplaceWorkingHoursAsync(_owner.Id, new[] { Request(DayOfWeek.Thursday, start, end) });

        result.Error!.Kind.Should().Be(ErrorKind.Validation);
        result.Error.Message.Should().Contain("Thursday");
        _hours.Verify(h => h.DeleteByCompanyIdAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task ReplaceWorkingHours_WithInactiveDayStartingAfterItsEnd_ShouldStillStoreIt()
    {
        var result = await _sut.ReplaceWorkingHoursAsync(_owner.Id, new[] { Request(DayOfWeek.Thursday, 18, 8, isActive: false) });

        result.IsSuccess.Should().BeTrue();
        _hours.Verify(h => h.CreateAsync(It.IsAny<WorkingHours>()), Times.Once);
    }

    [Fact]
    public async Task ReplaceWorkingHours_WithDuplicateDay_ShouldReturnValidationAndTouchNothing()
    {
        var result = await _sut.ReplaceWorkingHoursAsync(_owner.Id, new[]
        {
            Request(DayOfWeek.Monday, 8, 12),
            Request(DayOfWeek.Monday, 14, 18, isActive: false)
        });

        result.Error!.Kind.Should().Be(ErrorKind.Validation);
        result.Error.Message.Should().Contain("Monday");
        _hours.Verify(h => h.DeleteByCompanyIdAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Theory]
    [InlineData(9)]
    [InlineData(-1)]
    public async Task ReplaceWorkingHours_WithOutOfRangeDay_ShouldReturnValidationAndTouchNothing(int day)
    {
        var result = await _sut.ReplaceWorkingHoursAsync(_owner.Id, new[] { Request((DayOfWeek)day, 8, 12) });

        result.Error!.Kind.Should().Be(ErrorKind.Validation);
        _hours.Verify(h => h.DeleteByCompanyIdAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task ReplaceWorkingHours_WithEmptyList_ShouldOnlyClearTheSchedule()
    {
        var result = await _sut.ReplaceWorkingHoursAsync(_owner.Id, Array.Empty<WorkingHoursRequest>());

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().OnlyContain(d => !d.IsActive);
        _hours.Verify(h => h.DeleteByCompanyIdAsync(_companyId), Times.Once);
        _hours.Verify(h => h.CreateAsync(It.IsAny<WorkingHours>()), Times.Never);
    }
}
