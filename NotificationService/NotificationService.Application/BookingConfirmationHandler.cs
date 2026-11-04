using NotificationService.Application.Interfaces;
using NotificationService.Core;

namespace NotificationService.Application;

public sealed class BookingConfirmationHandler(IEmailSender emailSender) : INotificationHandler
{
    public bool CanHandle(NotificationEventType eventType) => eventType == NotificationEventType.BookingConfirmation;

    public Task HandleAsync(NotificationMessage message, CancellationToken cancellationToken = default)
    {
        var bookingReference = message.Data.GetValueOrDefault("bookingReference", "your booking");
        var serviceName = message.Data.GetValueOrDefault("serviceName", "requested service");
        var startTime = message.Data.GetValueOrDefault("startTime", "the scheduled time");

        return emailSender.SendAsync(
            new EmailMessage(
                message.RecipientEmail,
                message.RecipientName,
                $"Booking confirmation - {bookingReference}",
                $"<p>Hi {message.RecipientName},</p><p>Your booking for <strong>{serviceName}</strong> is confirmed.</p><p>Reference: <strong>{bookingReference}</strong><br />Time: {startTime}</p>",
                $"Hi {message.RecipientName},\n\nYour booking for {serviceName} is confirmed.\nReference: {bookingReference}\nTime: {startTime}"),
            cancellationToken);
    }
}
