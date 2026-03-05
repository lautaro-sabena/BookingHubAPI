using BookingHubAPI.Domain.Entities;
using BookingHubAPI.Domain.Interfaces;
using BookingHubAPI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BookingHubAPI.Infrastructure.Repositories;

public class WorkingHoursRepository : IWorkingHoursRepository
{
    private readonly BookingDbContext _context;

    public WorkingHoursRepository(BookingDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<WorkingHours>> GetByCompanyIdAsync(Guid companyId)
    {
        return await _context.WorkingHours
            .Where(wh => wh.CompanyId == companyId)
            .ToListAsync();
    }

    public async Task<WorkingHours> CreateAsync(WorkingHours workingHours)
    {
        workingHours.Id = Guid.NewGuid();
        workingHours.CreatedAt = DateTime.UtcNow;
        _context.WorkingHours.Add(workingHours);
        await _context.SaveChangesAsync();
        return workingHours;
    }

    public async Task<WorkingHours> UpdateAsync(WorkingHours workingHours)
    {
        workingHours.UpdatedAt = DateTime.UtcNow;
        _context.WorkingHours.Update(workingHours);
        await _context.SaveChangesAsync();
        return workingHours;
    }

    public async Task DeleteByCompanyIdAsync(Guid companyId)
    {
        var workingHours = await _context.WorkingHours
            .Where(wh => wh.CompanyId == companyId)
            .ToListAsync();
        
        _context.WorkingHours.RemoveRange(workingHours);
        await _context.SaveChangesAsync();
    }
}
