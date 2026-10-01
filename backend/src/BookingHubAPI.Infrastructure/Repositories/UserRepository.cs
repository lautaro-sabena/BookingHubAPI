using BookingHubAPI.Domain.Entities;
using BookingHubAPI.Domain.Interfaces;
using BookingHubAPI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BookingHubAPI.Infrastructure.Repositories;

public class UserRepository : IUserRepository
{
    private readonly BookingDbContext _context;

    public UserRepository(BookingDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetByIdAsync(Guid id)
    {
        return await _context.Users.FindAsync(id);
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        // E-mails are stored lower-cased (unique index on "Email"), so an exact match is enough and can use the index.
        var normalized = Normalize(email);
        return await _context.Users.FirstOrDefaultAsync(u => u.Email == normalized);
    }

    public async Task<IEnumerable<User>> GetAllAsync()
    {
        return await _context.Users.ToListAsync();
    }

    public async Task<User> CreateAsync(User user)
    {
        // A caller that must retry safely (registration) fixes the id beforehand.
        if (user.Id == Guid.Empty) user.Id = Guid.NewGuid();
        user.CreatedAt = DateTime.UtcNow;
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return user;
    }

    public async Task<User> UpdateAsync(User user)
    {
        user.UpdatedAt = DateTime.UtcNow;
        _context.Users.Update(user);
        await _context.SaveChangesAsync();
        return user;
    }

    public async Task<bool> ExistsAsync(string email)
    {
        var normalized = Normalize(email);
        return await _context.Users.AnyAsync(u => u.Email == normalized);
    }

    private static string Normalize(string email) => email.Trim().ToLowerInvariant();
}
