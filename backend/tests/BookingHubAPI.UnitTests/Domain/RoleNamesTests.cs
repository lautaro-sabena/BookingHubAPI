using BookingHubAPI.Domain.Entities;

namespace BookingHubAPI.UnitTests.Domain;

public class RoleNamesTests
{
    [Fact]
    public void RoleNames_ShouldMatchUserRoleEnumNames()
    {
        Assert.Equal(RoleNames.Owner, UserRole.Owner.ToString());
        Assert.Equal(RoleNames.Customer, UserRole.Customer.ToString());
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
