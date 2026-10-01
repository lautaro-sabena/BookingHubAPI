using BookingHubAPI.Application.DTOs;
using BookingHubAPI.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BookingHubAPI.API.Extensions;

namespace BookingHubAPI.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReservationsController : ControllerBase
{
    private readonly IReservationService _reservationService;

    public ReservationsController(IReservationService reservationService)
    {
        _reservationService = reservationService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<ReservationResponse>>> GetReservations(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? status = null)
    {
        var result = await _reservationService.GetReservationsAsync(User.GetUserId(), page, pageSize, status);
        return this.ToActionResult(result, Ok);
    }

    [HttpPost]
    public async Task<ActionResult<ReservationResponse>> CreateReservation([FromBody] ReservationRequest request)
    {
        var result = await _reservationService.CreateReservationAsync(User.GetUserId(), request);
        return this.ToActionResult(result, reservation => CreatedAtAction(nameof(GetReservations), reservation));
    }

    [HttpPut("{id}/confirm")]
    public async Task<ActionResult<ReservationResponse>> ConfirmReservation(Guid id)
    {
        var result = await _reservationService.ConfirmReservationAsync(User.GetUserId(), id);
        return this.ToActionResult(result, Ok);
    }

    [HttpPut("{id}/cancel")]
    public async Task<ActionResult<ReservationResponse>> CancelReservation(Guid id)
    {
        var result = await _reservationService.CancelReservationAsync(User.GetUserId(), id);
        return this.ToActionResult(result, Ok);
    }
}
