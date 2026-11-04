namespace BranchBooking.Application.Models.Notifications;

public enum NotificationEventType
{
    BookingConfirmation,
    StaffInvitation
}

public sealed record NotificationMessage(
    string EventId,
    NotificationEventType EventType,
    string RecipientEmail,
    string RecipientName,
    IReadOnlyDictionary<string, string> Data);
