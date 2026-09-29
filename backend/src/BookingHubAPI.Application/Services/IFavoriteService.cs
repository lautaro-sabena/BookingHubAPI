using BookingHubAPI.Application.Common;
using BookingHubAPI.Application.DTOs;

namespace BookingHubAPI.Application.Services;

/// <summary>Customer favorites use cases. Expected failures are returned as <see cref="Result"/> errors, never thrown.</summary>
public interface IFavoriteService
{
    Task<IReadOnlyList<FavoriteDto>> GetFavoritesAsync(Guid customerId);

    /// <summary>Favorites a service; a missing service is not found, a repeat is a validation error.</summary>
    Task<Result<FavoriteDto>> AddFavoriteAsync(Guid customerId, Guid serviceId);

    /// <summary>Removes the caller's own favorite; another customer's favorite is reported as not found.</summary>
    Task<Result> RemoveFavoriteAsync(Guid customerId, Guid serviceId);

    Task<bool> IsFavoriteAsync(Guid customerId, Guid serviceId);
}
