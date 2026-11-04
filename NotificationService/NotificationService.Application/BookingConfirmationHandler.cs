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
        var branchName = message.Data.GetValueOrDefault("branchName", "the selected branch");
        var branchAddress = string.Join(", ", new[]
        {
            message.Data.GetValueOrDefault("branchAddressLine1", string.Empty),
            message.Data.GetValueOrDefault("branchCity", string.Empty),
            message.Data.GetValueOrDefault("branchProvince", string.Empty),
            message.Data.GetValueOrDefault("branchPostalCode", string.Empty)
        }.Where(value => !string.IsNullOrWhiteSpace(value)));
        var locationText = string.IsNullOrWhiteSpace(branchAddress) ? branchName : $"{branchName}, {branchAddress}";
        var endTime = message.Data.GetValueOrDefault("endTime", string.Empty);
        var timeText = string.IsNullOrWhiteSpace(endTime) ? startTime : $"{startTime} - {endTime}";

        return emailSender.SendAsync(
            new EmailMessage(
                message.RecipientEmail,
                message.RecipientName,
                $"Booking confirmation - {bookingReference}",
                $"<p>Hi {message.RecipientName},</p><p>Your booking for <strong>{serviceName}</strong> is confirmed.</p><p>Reference: <strong>{bookingReference}</strong><br />Time: {timeText}<br />Location: {locationText}</p>",
                $"Hi {message.RecipientName},\n\nYour booking for {serviceName} is confirmed.\nReference: {bookingReference}\nTime: {timeText}\nLocation: {locationText}"),
            cancellationToken);
    }
}
