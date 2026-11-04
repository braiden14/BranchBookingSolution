using NotificationService.Application.Interfaces;
using NotificationService.Core;

namespace NotificationService.Application;

public sealed class NotificationHandler(
    IEnumerable<INotificationHandler> handlers)
{
    public Task HandleAsync(NotificationMessage message, CancellationToken cancellationToken = default)
    {
        var handler = handlers.FirstOrDefault(candidate => candidate.CanHandle(message.EventType));
        return handler is null
            ? throw new InvalidOperationException($"No notification handler is registered for event type '{message.EventType}'.")
            : handler.HandleAsync(message, cancellationToken);
    }
}
