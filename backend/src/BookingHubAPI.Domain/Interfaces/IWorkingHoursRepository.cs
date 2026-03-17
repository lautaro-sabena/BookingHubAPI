using BookingHubAPI.Domain.Entities;

namespace BookingHubAPI.Domain.Interfaces;

public interface IWorkingHoursRepository
{
    Task<IEnumerable<WorkingHours>> GetByCompanyIdAsync(Guid companyId);
    Task<WorkingHours> CreateAsync(WorkingHours workingHours);
    Task<WorkingHours> UpdateAsync(WorkingHours workingHours);
    Task DeleteByCompanyIdAsync(Guid companyId);
}
