using BookingHubAPI.Domain.Entities;

namespace BookingHubAPI.Application.DTOs;

public static class FavoriteMappingExtensions
{
    public static FavoriteDto ToDto(this Favorite favorite) => new()
    {
        Id = favorite.Id,
        ServiceId = favorite.Service.Id,
        ServiceName = favorite.Service.Name,
        ServiceDescription = favorite.Service.Description,
        DurationMinutes = favorite.Service.DurationMinutes,
        Price = favorite.Service.Price,
        CompanyId = favorite.Service.CompanyId,
        CompanyName = favorite.Service.Company.Name
    };
}
