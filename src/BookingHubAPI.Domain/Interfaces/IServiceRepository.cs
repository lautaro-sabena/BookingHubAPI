using BookingHubAPI.Domain.Entities;

namespace BookingHubAPI.Domain.Interfaces;

public interface IServiceRepository
{
    Task<Domain.Entities.Service?> GetByIdAsync(Guid id);
    Task<IEnumerable<Domain.Entities.Service>> GetByCompanyIdAsync(Guid companyId, int page, int pageSize, string? search = null);
    Task<int> GetCountByCompanyIdAsync(Guid companyId, string? search = null);
    Task<Domain.Entities.Service> CreateAsync(Domain.Entities.Service service);
    Task<Domain.Entities.Service> UpdateAsync(Domain.Entities.Service service);
    Task<bool> ExistsAsync(Guid id);
}
