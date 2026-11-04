namespace NotificationService.Core;

public sealed record NotificationMessage(
    string EventId,
    NotificationEventType EventType,
    string RecipientEmail,
    string RecipientName,
    IReadOnlyDictionary<string, string> Data);
