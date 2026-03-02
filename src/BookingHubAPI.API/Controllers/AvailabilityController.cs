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
    private readonly IReservationRepository _reservationRepository;

    public AvailabilityController(
        IServiceRepository serviceRepository,
        IReservationRepository reservationRepository)
    {
        _serviceRepository = serviceRepository;
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

        var dayOfWeek = date.DayOfWeek;
        var slots = new List<AvailableSlotResponse>();

        var startHour = 9;
        var endHour = 17;
        var slotDuration = service.DurationMinutes;

        for (var hour = startHour; hour < endHour; hour++)
        {
            for (var minute = 0; minute < 60; minute += slotDuration)
            {
                var slotStart = date.Date.AddHours(hour).AddMinutes(minute);
                var slotEnd = slotStart.AddMinutes(slotDuration);

                if (slotEnd > date.Date.AddHours(endHour))
                {
                    break;
                }

                var hasConflict = await _reservationRepository.HasConflictAsync(
                    service.CompanyId, serviceId, slotStart, slotEnd);

                slots.Add(new AvailableSlotResponse(slotStart, slotEnd, !hasConflict));
            }
        }

        return Ok(slots.Where(s => s.IsAvailable));
    }
}
