using BookingHubAPI.Domain.Entities;

namespace BookingHubAPI.UnitTests.Domain;

public class RoleNamesTests
{
    [Fact]
    public void RoleNames_ShouldMatchUserRoleEnumNames()
    {
        Assert.Equal(UserRole.Owner.ToString(), RoleNames.Owner);
        Assert.Equal(UserRole.Customer.ToString(), RoleNames.Customer);
    }

    [Fact]
    public void RoleNames_ShouldCoverEveryUserRole()
    {
        var constants = typeof(RoleNames)
            .GetFields()
            .Select(f => (string)f.GetRawConstantValue()!)
            .OrderBy(n => n);

        Assert.Equal(Enum.GetNames<UserRole>().OrderBy(n => n), constants);
    }
}
