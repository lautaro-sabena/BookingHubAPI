using BookingHubAPI.Application.Abstractions;
using BookingHubAPI.Application.Common;
using BookingHubAPI.Application.DTOs;
using BookingHubAPI.Domain.Entities;
using BookingHubAPI.Domain.Interfaces;
using FluentValidation;

namespace BookingHubAPI.Application.Services;

public class ReservationService : IReservationService
{
    private readonly IReservationRepository _reservationRepository;
    private readonly IServiceRepository _serviceRepository;
    private readonly ICompanyRepository _companyRepository;
    private readonly IUserRepository _userRepository;
    private readonly INotificationService _notificationService;
    private readonly IValidator<ReservationRequest> _requestValidator;

    public ReservationService(
        IReservationRepository reservationRepository,
        IServiceRepository serviceRepository,
        ICompanyRepository companyRepository,
        IUserRepository userRepository,
        INotificationService notificationService,
        IValidator<ReservationRequest> requestValidator)
    {
        _reservationRepository = reservationRepository;
        _serviceRepository = serviceRepository;
        _companyRepository = companyRepository;
        _userRepository = userRepository;
        _notificationService = notificationService;
        _requestValidator = requestValidator;
    }

    public async Task<Result<PagedResult<ReservationResponse>>> GetReservationsAsync(
        Guid userId, int page, int pageSize, string? status)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            return Error.NotFound("User not found");
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

        var responses = reservations.Select(r => ToResponse(r, r.Customer.Email, r.Service)).ToList();

        return new PagedResult<ReservationResponse>(responses, totalCount, page, pageSize, totalPages);
    }

    public async Task<Result<ReservationResponse>> CreateReservationAsync(Guid userId, ReservationRequest request)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            return Error.NotFound("User not found");
        }

        var service = await _serviceRepository.GetByIdAsync(request.ServiceId);
        if (service == null || !service.IsActive)
        {
            return Error.Validation("Service not found or inactive");
        }

        var company = await _companyRepository.GetByIdAsync(service.CompanyId);
        if (company == null || !company.IsActive)
        {
            return Error.Validation("Company not found or inactive");
        }

        var endTime = request.StartTime.AddMinutes(service.DurationMinutes);

        if (await _reservationRepository.HasConflictAsync(service.CompanyId, service.Id, request.StartTime, endTime))
        {
            return Error.Conflict("Time slot is not available");
        }

        // Runs after the availability check on purpose: it preserves the original precedence of
        // error responses (a request that is both taken and in the past reports the conflict).
        var validation = await _requestValidator.ValidateAsync(request);
        if (!validation.IsValid)
        {
            return Error.Validation(validation.Errors[0].ErrorMessage);
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

        return ToResponse(createdReservation, user.Email, service);
    }

    public async Task<Result<ReservationResponse>> ConfirmReservationAsync(Guid userId, Guid reservationId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null || user.Role != UserRole.Owner || !user.CompanyId.HasValue)
        {
            return Error.Forbidden("Only company owners can confirm reservations");
        }

        var reservation = await _reservationRepository.GetByIdAsync(reservationId);
        // A reservation of another company is reported as missing so its existence is not disclosed.
        if (reservation == null || reservation.CompanyId != user.CompanyId.Value)
        {
            return Error.NotFound("Reservation not found");
        }

        if (reservation.Status != ReservationStatus.Pending)
        {
            return Error.Validation("Only pending reservations can be confirmed");
        }

        reservation.Status = ReservationStatus.Confirmed;
        var updatedReservation = await _reservationRepository.UpdateAsync(reservation);

        var customer = await _userRepository.GetByIdAsync(reservation.CustomerId);
        if (customer != null)
        {
            await _notificationService.SendReservationConfirmedAsync(reservation.Id, customer.Email);
        }

        return ToResponse(updatedReservation, customer?.Email ?? string.Empty, reservation.Service);
    }

    public async Task<Result<ReservationResponse>> CancelReservationAsync(Guid userId, Guid reservationId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            return Error.NotFound("User not found");
        }

        var reservation = await _reservationRepository.GetByIdAsync(reservationId);
        if (reservation == null)
        {
            return Error.NotFound("Reservation not found");
        }

        if (user.Role == UserRole.Customer && reservation.CustomerId != userId)
        {
            return Error.Forbidden("Reservation belongs to another customer");
        }

        if (user.Role == UserRole.Owner && user.CompanyId.HasValue && reservation.CompanyId != user.CompanyId.Value)
        {
            return Error.Forbidden("Reservation belongs to another company");
        }

        if (reservation.Status == ReservationStatus.Cancelled || reservation.Status == ReservationStatus.Completed)
        {
            return Error.Validation("Reservation cannot be cancelled");
        }

        reservation.Status = ReservationStatus.Cancelled;
        var updatedReservation = await _reservationRepository.UpdateAsync(reservation);

        var customer = await _userRepository.GetByIdAsync(reservation.CustomerId);
        if (customer != null)
        {
            await _notificationService.SendReservationCancelledAsync(reservation.Id, customer.Email, "Reservation cancelled");
        }

        return ToResponse(updatedReservation, customer?.Email ?? string.Empty, reservation.Service);
    }

    private static ReservationResponse ToResponse(Reservation reservation, string customerEmail, Domain.Entities.Service service) =>
        new(
            reservation.Id,
            reservation.CustomerId,
            customerEmail,
            reservation.ServiceId,
            service.Name,
            service.DurationMinutes,
            reservation.StartTime,
            reservation.EndTime,
            reservation.Status.ToString(),
            reservation.Notes,
            reservation.CreatedAt);
}
