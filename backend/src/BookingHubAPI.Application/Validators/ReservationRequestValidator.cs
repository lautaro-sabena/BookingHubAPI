using BookingHubAPI.Application.DTOs;
using FluentValidation;

namespace BookingHubAPI.Application.Validators;

/// <summary>
/// Business validation of a booking request. Structural rules (required fields, notes length)
/// are enforced by the DTO data annotations at model binding.
/// </summary>
public class ReservationRequestValidator : AbstractValidator<ReservationRequest>
{
    public const string PastBookingMessage = "Cannot book in the past";

    public ReservationRequestValidator(TimeProvider timeProvider)
    {
        RuleFor(r => r.StartTime)
            .Must(startTime => startTime >= timeProvider.GetUtcNow())
            .WithMessage(PastBookingMessage);
    }
}
