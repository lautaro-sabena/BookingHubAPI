using BookingHubAPI.Domain.Entities;
using BookingHubAPI.Domain.Interfaces;
using BookingHubAPI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BookingHubAPI.Infrastructure.Repositories;

public class ServiceRepository : IServiceRepository
{
    private readonly BookingDbContext _context;

    public ServiceRepository(BookingDbContext context)
    {
        _context = context;
    }

    public async Task<Domain.Entities.Service?> GetByIdAsync(Guid id)
    {
        return await _context.Services.FindAsync(id);
    }

    public async Task<IEnumerable<Domain.Entities.Service>> GetByCompanyIdAsync(Guid companyId, int page, int pageSize, string? search = null)
    {
        var query = _context.Services
            .Where(s => s.CompanyId == companyId && s.IsActive);

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(s => s.Name.Contains(search));
        }

        return await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetCountByCompanyIdAsync(Guid companyId, string? search = null)
    {
        var query = _context.Services
            .Where(s => s.CompanyId == companyId && s.IsActive);

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(s => s.Name.Contains(search));
        }

        return await query.CountAsync();
    }

    public async Task<Domain.Entities.Service> CreateAsync(Domain.Entities.Service service)
    {
        service.Id = Guid.NewGuid();
        service.CreatedAt = DateTime.UtcNow;
        _context.Services.Add(service);
        await _context.SaveChangesAsync();
        return service;
    }

    public async Task<Domain.Entities.Service> UpdateAsync(Domain.Entities.Service service)
    {
        service.UpdatedAt = DateTime.UtcNow;
        _context.Services.Update(service);
        await _context.SaveChangesAsync();
        return service;
    }

    public async Task<bool> ExistsAsync(Guid id)
    {
        return await _context.Services.AnyAsync(s => s.Id == id);
    }
}
