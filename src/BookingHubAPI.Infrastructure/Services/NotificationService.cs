using Microsoft.Extensions.Logging;

namespace BookingHubAPI.Infrastructure.Services;

public interface INotificationService
{
    Task SendEmailAsync(string to, string subject, string body);
    Task SendReservationCreatedAsync(Guid reservationId, string customerEmail, string companyName);
    Task SendReservationConfirmedAsync(Guid reservationId, string customerEmail);
    Task SendReservationCancelledAsync(Guid reservationId, string customerEmail, string reason);
}

public class NotificationService : INotificationService
{
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(ILogger<NotificationService> logger)
    {
        _logger = logger;
    }

    public Task SendEmailAsync(string to, string subject, string body)
    {
        _logger.LogInformation("MOCK EMAIL to: {To}, Subject: {Subject}, Body: {Body}", to, subject, body);
        return Task.CompletedTask;
    }

    public Task SendReservationCreatedAsync(Guid reservationId, string customerEmail, string companyName)
    {
        _logger.LogInformation(
            "Reservation Created - ReservationId: {ReservationId}, Customer: {CustomerEmail}, Company: {CompanyName}",
            reservationId, customerEmail, companyName);
        return Task.CompletedTask;
    }

    public Task SendReservationConfirmedAsync(Guid reservationId, string customerEmail)
    {
        _logger.LogInformation(
            "Reservation Confirmed - ReservationId: {ReservationId}, Customer: {CustomerEmail}",
            reservationId, customerEmail);
        return Task.CompletedTask;
    }

    public Task SendReservationCancelledAsync(Guid reservationId, string customerEmail, string reason)
    {
        _logger.LogInformation(
            "Reservation Cancelled - ReservationId: {ReservationId}, Customer: {CustomerEmail}, Reason: {Reason}",
            reservationId, customerEmail, reason);
        return Task.CompletedTask;
    }
}
