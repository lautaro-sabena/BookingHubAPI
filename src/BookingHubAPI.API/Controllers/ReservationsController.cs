using AutoMapper;
using BookingHubAPI.Application.DTOs;
using BookingHubAPI.Domain.Entities;
using BookingHubAPI.Domain.Interfaces;
using BookingHubAPI.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BookingHubAPI.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReservationsController : ControllerBase
{
    private readonly IReservationRepository _reservationRepository;
    private readonly IServiceRepository _serviceRepository;
    private readonly ICompanyRepository _companyRepository;
    private readonly IUserRepository _userRepository;
    private readonly INotificationService _notificationService;
    private readonly IMapper _mapper;

    public ReservationsController(
        IReservationRepository reservationRepository,
        IServiceRepository serviceRepository,
        ICompanyRepository companyRepository,
        IUserRepository userRepository,
        INotificationService notificationService,
        IMapper mapper)
    {
        _reservationRepository = reservationRepository;
        _serviceRepository = serviceRepository;
        _companyRepository = companyRepository;
        _userRepository = userRepository;
        _notificationService = notificationService;
        _mapper = mapper;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<ReservationResponse>>> GetReservations(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? status = null)
    {
        var userId = GetUserId();
        var user = await _userRepository.GetByIdAsync(userId);

        if (user == null)
        {
            return NotFound();
        }

        ReservationStatus? statusFilter = null;
        if (!string.IsNullOrEmpty(status) && Enum.TryParse<ReservationStatus>(status, true, out var parsedStatus))
        {
            statusFilter = parsedStatus;
        }

        IEnumerable<Reservation> reservations;
        int totalCount;

        if (user.Role == UserRole.Owner && user.CompanyId.HasValue)
        {
            reservations = await _reservationRepository.GetByCompanyIdAsync(user.CompanyId.Value, page, pageSize, statusFilter);
            totalCount = await _reservationRepository.GetCountByCompanyIdAsync(user.CompanyId.Value, statusFilter);
        }
        else
        {
            reservations = await _reservationRepository.GetByCustomerIdAsync(userId, page, pageSize, statusFilter);
            totalCount = await _reservationRepository.GetCountByCustomerIdAsync(userId, statusFilter);
        }

        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var responses = reservations.Select(r => new ReservationResponse(
            r.Id, r.CustomerId, r.ServiceId, r.Service.Name, r.Service.DurationMinutes,
            r.StartTime, r.EndTime, r.Status.ToString(), r.Notes, r.CreatedAt)).ToList();

        return Ok(new PagedResult<ReservationResponse>(responses, totalCount, page, pageSize, totalPages));
    }

    [HttpPost]
    public async Task<ActionResult<ReservationResponse>> CreateReservation([FromBody] ReservationRequest request)
    {
        var userId = GetUserId();
        var user = await _userRepository.GetByIdAsync(userId);

        if (user == null)
        {
            return NotFound();
        }

        var service = await _serviceRepository.GetByIdAsync(request.ServiceId);
        if (service == null || !service.IsActive)
        {
            return BadRequest(new { error = "Service not found or inactive" });
        }

        var company = await _companyRepository.GetByIdAsync(service.CompanyId);
        if (company == null || !company.IsActive)
        {
            return BadRequest(new { error = "Company not found or inactive" });
        }

        var endTime = request.StartTime.AddMinutes(service.DurationMinutes);

        if (await _reservationRepository.HasConflictAsync(service.CompanyId, service.Id, request.StartTime, endTime))
        {
            return Conflict(new { error = "Time slot is not available" });
        }

        if (request.StartTime < DateTime.UtcNow)
        {
            return BadRequest(new { error = "Cannot book in the past" });
        }

        var reservation = new Reservation
        {
            CustomerId = userId,
            ServiceId = request.ServiceId,
            CompanyId = service.CompanyId,
            StartTime = request.StartTime,
            EndTime = endTime,
            Status = ReservationStatus.Pending,
            Notes = request.Notes
        };

        var createdReservation = await _reservationRepository.CreateAsync(reservation);
        createdReservation.Service = service;
        createdReservation.Company = company;

        await _notificationService.SendReservationCreatedAsync(createdReservation.Id, user.Email, company.Name);

        return CreatedAtAction(nameof(GetReservations), new ReservationResponse(
            createdReservation.Id, createdReservation.CustomerId, createdReservation.ServiceId,
            service.Name, service.DurationMinutes, createdReservation.StartTime, createdReservation.EndTime,
            createdReservation.Status.ToString(), createdReservation.Notes, createdReservation.CreatedAt));
    }

    [HttpPut("{id}/confirm")]
    public async Task<ActionResult<ReservationResponse>> ConfirmReservation(Guid id)
    {
        var userId = GetUserId();
        var user = await _userRepository.GetByIdAsync(userId);

        if (user == null || user.Role != UserRole.Owner || !user.CompanyId.HasValue)
        {
            return Forbid();
        }

        var reservation = await _reservationRepository.GetByIdAsync(id);
        if (reservation == null || reservation.CompanyId != user.CompanyId.Value)
        {
            return NotFound();
        }

        if (reservation.Status != ReservationStatus.Pending)
        {
            return BadRequest(new { error = "Only pending reservations can be confirmed" });
        }

        reservation.Status = ReservationStatus.Confirmed;
        var updatedReservation = await _reservationRepository.UpdateAsync(reservation);

        var customer = await _userRepository.GetByIdAsync(reservation.CustomerId);
        if (customer != null)
        {
            await _notificationService.SendReservationConfirmedAsync(reservation.Id, customer.Email);
        }

        return Ok(new ReservationResponse(
            updatedReservation.Id, updatedReservation.CustomerId, updatedReservation.ServiceId,
            reservation.Service.Name, reservation.Service.DurationMinutes, updatedReservation.StartTime,
            updatedReservation.EndTime, updatedReservation.Status.ToString(), updatedReservation.Notes,
            updatedReservation.CreatedAt));
    }

    [HttpPut("{id}/cancel")]
    public async Task<ActionResult<ReservationResponse>> CancelReservation(Guid id)
    {
        var userId = GetUserId();
        var user = await _userRepository.GetByIdAsync(userId);

        if (user == null)
        {
            return NotFound();
        }

        var reservation = await _reservationRepository.GetByIdAsync(id);
        if (reservation == null)
        {
            return NotFound();
        }

        if (user.Role == UserRole.Customer && reservation.CustomerId != userId)
        {
            return Forbid();
        }

        if (user.Role == UserRole.Owner && user.CompanyId.HasValue && reservation.CompanyId != user.CompanyId.Value)
        {
            return Forbid();
        }

        if (reservation.Status == ReservationStatus.Cancelled || reservation.Status == ReservationStatus.Completed)
        {
            return BadRequest(new { error = "Reservation cannot be cancelled" });
        }

        reservation.Status = ReservationStatus.Cancelled;
        var updatedReservation = await _reservationRepository.UpdateAsync(reservation);

        var customer = await _userRepository.GetByIdAsync(reservation.CustomerId);
        if (customer != null)
        {
            await _notificationService.SendReservationCancelledAsync(reservation.Id, customer.Email, "Reservation cancelled");
        }

        return Ok(new ReservationResponse(
            updatedReservation.Id, updatedReservation.CustomerId, updatedReservation.ServiceId,
            reservation.Service.Name, reservation.Service.DurationMinutes, updatedReservation.StartTime,
            updatedReservation.EndTime, updatedReservation.Status.ToString(), updatedReservation.Notes,
            updatedReservation.CreatedAt));
    }

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.Parse(userIdClaim!);
    }
}
