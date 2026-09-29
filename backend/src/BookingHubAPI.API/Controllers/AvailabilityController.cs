using BookingHubAPI.API.Extensions;
using BookingHubAPI.Application.DTOs;
using BookingHubAPI.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookingHubAPI.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AvailabilityController : ControllerBase
{
    private readonly IAvailabilityService _availabilityService;

    public AvailabilityController(IAvailabilityService availabilityService)
    {
        _availabilityService = availabilityService;
    }

    [HttpGet("{serviceId}")]
    public async Task<ActionResult<IEnumerable<AvailableSlotResponse>>> GetAvailability(
        Guid serviceId,
        [FromQuery] DateTime date)
    {
        var result = await _availabilityService.GetAvailableSlotsAsync(serviceId, date);
        return this.ToActionResult(result, Ok);
    }
}
