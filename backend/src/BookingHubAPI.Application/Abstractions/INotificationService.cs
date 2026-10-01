namespace BookingHubAPI.Application.Abstractions;

/// <summary>Outbound notifications; implemented by the infrastructure layer.</summary>
public interface INotificationService
{
    Task SendEmailAsync(string to, string subject, string body);
    Task SendReservationCreatedAsync(Guid reservationId, string customerEmail, string companyName);
    Task SendReservationConfirmedAsync(Guid reservationId, string customerEmail);
    Task SendReservationCancelledAsync(Guid reservationId, string customerEmail, string reason);
}
