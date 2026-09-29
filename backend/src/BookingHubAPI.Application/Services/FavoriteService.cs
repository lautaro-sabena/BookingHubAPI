using BookingHubAPI.Application.Common;
using BookingHubAPI.Application.DTOs;
using BookingHubAPI.Domain.Entities;
using BookingHubAPI.Domain.Interfaces;

namespace BookingHubAPI.Application.Services;

public class FavoriteService : IFavoriteService
{
    private readonly IFavoriteRepository _favoriteRepository;
    private readonly IServiceRepository _serviceRepository;

    public FavoriteService(IFavoriteRepository favoriteRepository, IServiceRepository serviceRepository)
    {
        _favoriteRepository = favoriteRepository;
        _serviceRepository = serviceRepository;
    }

    public async Task<IReadOnlyList<FavoriteDto>> GetFavoritesAsync(Guid customerId)
    {
        var favorites = await _favoriteRepository.GetByCustomerIdAsync(customerId);
        return favorites.Select(f => f.ToDto()).ToList();
    }

    public async Task<Result<FavoriteDto>> AddFavoriteAsync(Guid customerId, Guid serviceId)
    {
        var service = await _serviceRepository.GetByIdWithCompanyAsync(serviceId);
        if (service == null)
        {
            return Error.NotFound("Service not found");
        }

        if (await _favoriteRepository.GetByCustomerAndServiceAsync(customerId, serviceId) != null)
        {
            return Error.Validation("Service already in favorites");
        }

        var created = await _favoriteRepository.AddAsync(new Favorite
        {
            CustomerId = customerId,
            ServiceId = serviceId
        });

        // The repository returns the bare entity; the service (with its company) is already loaded.
        created.Service = service;
        return created.ToDto();
    }

    public async Task<Result> RemoveFavoriteAsync(Guid customerId, Guid serviceId)
    {
        var removed = await _favoriteRepository.RemoveAsync(customerId, serviceId);
        return removed ? Result.Success() : Result.Failure(Error.NotFound("Favorite not found"));
    }

    public Task<bool> IsFavoriteAsync(Guid customerId, Guid serviceId) =>
        _favoriteRepository.ExistsAsync(customerId, serviceId);
}
