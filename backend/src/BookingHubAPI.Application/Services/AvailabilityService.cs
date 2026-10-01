using BookingHubAPI.Application.Common;
using BookingHubAPI.Application.DTOs;
using BookingHubAPI.Domain.Interfaces;
using BookingHubAPI.Domain.Scheduling;

namespace BookingHubAPI.Application.Services;

public class AvailabilityService : IAvailabilityService
{
    private readonly IServiceRepository _serviceRepository;
    private readonly ICompanyRepository _companyRepository;
    private readonly IReservationRepository _reservationRepository;
    private readonly TimeProvider _timeProvider;

    public AvailabilityService(
        IServiceRepository serviceRepository,
        ICompanyRepository companyRepository,
        IReservationRepository reservationRepository,
        TimeProvider timeProvider)
    {
        _serviceRepository = serviceRepository;
        _companyRepository = companyRepository;
        _reservationRepository = reservationRepository;
        _timeProvider = timeProvider;
    }

    public async Task<Result<IReadOnlyList<AvailableSlotResponse>>> GetAvailableSlotsAsync(Guid serviceId, DateTime date)
    {
        var service = await _serviceRepository.GetByIdAsync(serviceId);
        if (service == null || !service.IsActive)
        {
            return Error.NotFound("Service not found or inactive");
        }

        var company = await _companyRepository.GetByIdWithWorkingHoursAsync(service.CompanyId);
        if (company == null || !company.IsActive)
        {
            return Error.NotFound("Company not found or inactive");
        }

        // Only the calendar day of `date` matters: it is a day in the company's time zone.
        var day = DateOnly.FromDateTime(date);
        var zone = BookingSchedule.ResolveTimeZone(company.TimeZone);
        var slots = new List<AvailableSlotResponse>();
        var hours = BookingSchedule.FindActiveHours(company.WorkingHours, day.DayOfWeek);
        var duration = TimeSpan.FromMinutes(service.DurationMinutes);
        if (hours == null || duration <= TimeSpan.Zero)
        {
            return slots;
        }

        var (open, close) = BookingSchedule.WindowOn(hours, day, zone);
        var now = _timeProvider.GetUtcNow();

        for (var slotStart = open; slotStart + duration <= close; slotStart += duration)
        {
            // Same boundary as the booking rule: a slot starting exactly now can still be booked.
            if (slotStart < now)
            {
                continue;
            }

            var slotEnd = slotStart + duration;
            if (!await _reservationRepository.HasConflictAsync(
                    service.CompanyId, serviceId, slotStart.UtcDateTime, slotEnd.UtcDateTime))
            {
                slots.Add(new AvailableSlotResponse(
                    TimeZoneInfo.ConvertTime(slotStart, zone), TimeZoneInfo.ConvertTime(slotEnd, zone), true));
            }
        }

        return slots;
    }
}
