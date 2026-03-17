using BookingHubAPI.Domain.Entities;

namespace BookingHubAPI.Domain.Interfaces;

public interface IReservationRepository
{
    Task<Reservation?> GetByIdAsync(Guid id);
    Task<IEnumerable<Reservation>> GetByCustomerIdAsync(Guid customerId, int page, int pageSize, ReservationStatus? status = null);
    Task<IEnumerable<Reservation>> GetByCompanyIdAsync(Guid companyId, int page, int pageSize, ReservationStatus? status = null);
    Task<int> GetCountByCustomerIdAsync(Guid customerId, ReservationStatus? status = null);
    Task<int> GetCountByCompanyIdAsync(Guid companyId, ReservationStatus? status = null);
    Task<bool> HasConflictAsync(Guid companyId, Guid serviceId, DateTime startTime, DateTime endTime, Guid? excludeReservationId = null);
    Task<Reservation> CreateAsync(Reservation reservation);
    Task<Reservation> UpdateAsync(Reservation reservation);
}
