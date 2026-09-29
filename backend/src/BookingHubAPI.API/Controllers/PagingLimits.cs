namespace BookingHubAPI.API.Controllers;

/// <summary>Bounds for the <c>page</c> / <c>pageSize</c> query parameters of every paged endpoint.</summary>
public static class PagingLimits
{
    /// <summary>Keeps <c>(page - 1) * pageSize</c> comfortably inside <see cref="int"/>.</summary>
    public const int MaxPage = 1_000_000;

    /// <summary>Largest page a client may request; the UI uses the default of 10.</summary>
    public const int MaxPageSize = 100;
}
