using BookingHubAPI.Domain.Entities;

namespace BookingHubAPI.Domain.Interfaces;

public interface IFavoriteRepository
{
    Task<IEnumerable<Favorite>> GetByCustomerIdAsync(Guid customerId);
    Task<Favorite?> GetByCustomerAndServiceAsync(Guid customerId, Guid serviceId);
    Task<Favorite> AddAsync(Favorite favorite);
    Task<bool> RemoveAsync(Guid customerId, Guid serviceId);
    Task<bool> ExistsAsync(Guid customerId, Guid serviceId);
}