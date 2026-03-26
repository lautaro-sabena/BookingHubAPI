using BookingHubAPI.Domain.Entities;
using BookingHubAPI.Domain.Interfaces;
using BookingHubAPI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BookingHubAPI.Infrastructure.Repositories;

public class FavoriteRepository : IFavoriteRepository
{
    private readonly BookingDbContext _context;

    public FavoriteRepository(BookingDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Favorite>> GetByCustomerIdAsync(Guid customerId)
    {
        return await _context.Favorites
            .Include(f => f.Service)
                .ThenInclude(s => s.Company)
            .Where(f => f.CustomerId == customerId)
            .ToListAsync();
    }

    public async Task<Favorite?> GetByCustomerAndServiceAsync(Guid customerId, Guid serviceId)
    {
        return await _context.Favorites
            .FirstOrDefaultAsync(f => f.CustomerId == customerId && f.ServiceId == serviceId);
    }

    public async Task<Favorite> AddAsync(Favorite favorite)
    {
        favorite.Id = Guid.NewGuid();
        favorite.CreatedAt = DateTime.UtcNow;
        _context.Favorites.Add(favorite);
        await _context.SaveChangesAsync();
        return favorite;
    }

    public async Task<bool> RemoveAsync(Guid customerId, Guid serviceId)
    {
        var favorite = await _context.Favorites
            .FirstOrDefaultAsync(f => f.CustomerId == customerId && f.ServiceId == serviceId);
        
        if (favorite == null) return false;

        _context.Favorites.Remove(favorite);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ExistsAsync(Guid customerId, Guid serviceId)
    {
        return await _context.Favorites
            .AnyAsync(f => f.CustomerId == customerId && f.ServiceId == serviceId);
    }
}