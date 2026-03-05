using BookingHubAPI.Domain.Entities;
using BookingHubAPI.Domain.Interfaces;
using BookingHubAPI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BookingHubAPI.Infrastructure.Repositories;

public class CompanyRepository : ICompanyRepository
{
    private readonly BookingDbContext _context;

    public CompanyRepository(BookingDbContext context)
    {
        _context = context;
    }

    public async Task<Company?> GetByIdAsync(Guid id)
    {
        return await _context.Companies
            .Include(c => c.Owner)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<Company?> GetByIdWithWorkingHoursAsync(Guid id)
    {
        return await _context.Companies
            .Include(c => c.WorkingHours)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<Company?> GetByOwnerIdAsync(Guid ownerId)
    {
        return await _context.Companies
            .Include(c => c.Services)
            .Include(c => c.WorkingHours)
            .FirstOrDefaultAsync(c => c.OwnerId == ownerId);
    }

    public async Task<IEnumerable<Company>> GetAllAsync(int page, int pageSize)
    {
        return await _context.Companies
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<Company> CreateAsync(Company company)
    {
        company.Id = Guid.NewGuid();
        company.CreatedAt = DateTime.UtcNow;
        _context.Companies.Add(company);
        await _context.SaveChangesAsync();
        return company;
    }

    public async Task<Company> UpdateAsync(Company company)
    {
        company.UpdatedAt = DateTime.UtcNow;
        _context.Companies.Update(company);
        await _context.SaveChangesAsync();
        return company;
    }

    public async Task<bool> ExistsAsync(Guid id)
    {
        return await _context.Companies.AnyAsync(c => c.Id == id);
    }
}
