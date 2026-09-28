namespace BookingHubAPI.Domain.Entities;

/// <summary>
/// Compile-time role names derived from <see cref="UserRole"/>, usable in
/// <c>[Authorize(Roles = RoleNames.Owner)]</c>. They match the values emitted
/// as role claims in issued JWTs (<c>UserRole.ToString()</c>).
/// </summary>
public static class RoleNames
{
    public const string Owner = nameof(UserRole.Owner);
    public const string Customer = nameof(UserRole.Customer);
}
