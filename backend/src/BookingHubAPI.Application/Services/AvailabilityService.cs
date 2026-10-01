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

    public AvailabilityService(
        IServiceRepository serviceRepository,
        ICompanyRepository companyRepository,
        IReservationRepository reservationRepository)
    {
        _serviceRepository = serviceRepository;
        _companyRepository = companyRepository;
        _reservationRepository = reservationRepository;
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

        var slots = new List<AvailableSlotResponse>();
        var hours = BookingSchedule.FindActiveHours(company.WorkingHours, date.DayOfWeek);
        var duration = TimeSpan.FromMinutes(service.DurationMinutes);
        if (hours == null || duration <= TimeSpan.Zero)
        {
            return slots;
        }

        var (open, close) = BookingSchedule.WindowOn(hours, date);

        for (var slotStart = open; slotStart + duration <= close; slotStart += duration)
        {
            var slotEnd = slotStart + duration;
            if (!await _reservationRepository.HasConflictAsync(service.CompanyId, serviceId, slotStart, slotEnd))
            {
                slots.Add(new AvailableSlotResponse(slotStart, slotEnd, true));
            }
        }

        return slots;
    }
}
