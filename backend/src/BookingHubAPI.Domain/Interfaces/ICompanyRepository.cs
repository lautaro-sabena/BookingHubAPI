using BookingHubAPI.Domain.Entities;

namespace BookingHubAPI.Domain.Interfaces;

public interface ICompanyRepository
{
    Task<Company?> GetByIdAsync(Guid id);
    Task<Company?> GetByIdWithWorkingHoursAsync(Guid id);
    Task<Company?> GetByOwnerIdAsync(Guid ownerId);
    Task<IEnumerable<Company>> GetAllAsync(int page, int pageSize);
    Task<Company> CreateAsync(Company company);
    Task<Company> UpdateAsync(Company company);
    Task<bool> ExistsAsync(Guid id);
}
