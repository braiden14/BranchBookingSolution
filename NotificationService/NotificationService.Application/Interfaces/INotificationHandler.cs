using NotificationService.Core;

namespace NotificationService.Application.Interfaces;

public interface INotificationHandler
{
    bool CanHandle(NotificationEventType eventType);
    Task HandleAsync(NotificationMessage message, CancellationToken cancellationToken = default);
}
