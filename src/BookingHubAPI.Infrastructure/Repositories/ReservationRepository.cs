using BookingHubAPI.Domain.Entities;
using BookingHubAPI.Domain.Interfaces;
using BookingHubAPI.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BookingHubAPI.Infrastructure.Repositories;

public class ReservationRepository : IReservationRepository
{
    private readonly BookingDbContext _context;

    public ReservationRepository(BookingDbContext context)
    {
        _context = context;
    }

    public async Task<Reservation?> GetByIdAsync(Guid id)
    {
        return await _context.Reservations
            .Include(r => r.Service)
            .Include(r => r.Company)
            .Include(r => r.Customer)
            .FirstOrDefaultAsync(r => r.Id == id);
    }

    public async Task<IEnumerable<Reservation>> GetByCustomerIdAsync(Guid customerId, int page, int pageSize, ReservationStatus? status = null)
    {
        var query = _context.Reservations
            .Include(r => r.Service)
            .Where(r => r.CustomerId == customerId);

        if (status.HasValue)
        {
            query = query.Where(r => r.Status == status.Value);
        }

        return await query
            .OrderByDescending(r => r.StartTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<IEnumerable<Reservation>> GetByCompanyIdAsync(Guid companyId, int page, int pageSize, ReservationStatus? status = null)
    {
        var query = _context.Reservations
            .Include(r => r.Service)
            .Include(r => r.Customer)
            .Where(r => r.CompanyId == companyId);

        if (status.HasValue)
        {
            query = query.Where(r => r.Status == status.Value);
        }

        return await query
            .OrderByDescending(r => r.StartTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetCountByCustomerIdAsync(Guid customerId, ReservationStatus? status = null)
    {
        var query = _context.Reservations.Where(r => r.CustomerId == customerId);

        if (status.HasValue)
        {
            query = query.Where(r => r.Status == status.Value);
        }

        return await query.CountAsync();
    }

    public async Task<int> GetCountByCompanyIdAsync(Guid companyId, ReservationStatus? status = null)
    {
        var query = _context.Reservations.Where(r => r.CompanyId == companyId);

        if (status.HasValue)
        {
            query = query.Where(r => r.Status == status.Value);
        }

        return await query.CountAsync();
    }

    public async Task<bool> HasConflictAsync(Guid companyId, Guid serviceId, DateTime startTime, DateTime endTime, Guid? excludeReservationId = null)
    {
        var query = _context.Reservations
            .Where(r => r.CompanyId == companyId
                && r.ServiceId == serviceId
                && r.Status != ReservationStatus.Cancelled
                && r.StartTime < endTime
                && r.EndTime > startTime);

        if (excludeReservationId.HasValue)
        {
            query = query.Where(r => r.Id != excludeReservationId.Value);
        }

        return await query.AnyAsync();
    }

    public async Task<Reservation> CreateAsync(Reservation reservation)
    {
        reservation.Id = Guid.NewGuid();
        reservation.CreatedAt = DateTime.UtcNow;
        _context.Reservations.Add(reservation);
        await _context.SaveChangesAsync();
        return reservation;
    }

    public async Task<Reservation> UpdateAsync(Reservation reservation)
    {
        reservation.UpdatedAt = DateTime.UtcNow;
        _context.Reservations.Update(reservation);
        await _context.SaveChangesAsync();
        return reservation;
    }
}
