using BookingHubAPI.Application.DTOs;
using BookingHubAPI.Domain.Entities;
using BookingHubAPI.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;

namespace BookingHubAPI.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AvailabilityController : ControllerBase
{
    private readonly IServiceRepository _serviceRepository;
    private readonly ICompanyRepository _companyRepository;
    private readonly IReservationRepository _reservationRepository;

    public AvailabilityController(
        IServiceRepository serviceRepository,
        ICompanyRepository companyRepository,
        IReservationRepository reservationRepository)
    {
        _serviceRepository = serviceRepository;
        _companyRepository = companyRepository;
        _reservationRepository = reservationRepository;
    }

    [HttpGet("{serviceId}")]
    public async Task<ActionResult<IEnumerable<AvailableSlotResponse>>> GetAvailability(
        Guid serviceId,
        [FromQuery] DateTime date)
    {
        var service = await _serviceRepository.GetByIdAsync(serviceId);
        if (service == null || !service.IsActive)
        {
            return NotFound(new { error = "Service not found or inactive" });
        }

        var company = await _companyRepository.GetByIdWithWorkingHoursAsync(service.CompanyId);
        if (company == null || !company.IsActive)
        {
            return NotFound(new { error = "Company not found or inactive" });
        }

        var dayOfWeek = date.DayOfWeek;
        var workingHours = company.WorkingHours.FirstOrDefault(wh => wh.DayOfWeek == dayOfWeek && wh.IsActive);

        if (workingHours == null)
        {
            return Ok(new List<AvailableSlotResponse>());
        }

        var slots = new List<AvailableSlotResponse>();
        var startTime = workingHours.StartTime;
        var endTime = workingHours.EndTime;
        var slotDuration = service.DurationMinutes;

        var currentSlot = date.Date.Add(startTime);
        var endDateTime = date.Date.Add(endTime);

        while (currentSlot.AddMinutes(slotDuration) <= endDateTime)
        {
            var slotEnd = currentSlot.AddMinutes(slotDuration);

            var hasConflict = await _reservationRepository.HasConflictAsync(
                service.CompanyId, serviceId, currentSlot, slotEnd);

            slots.Add(new AvailableSlotResponse(currentSlot, slotEnd, !hasConflict));

            currentSlot = currentSlot.AddMinutes(slotDuration);
        }

        return Ok(slots.Where(s => s.IsAvailable));
    }
}
