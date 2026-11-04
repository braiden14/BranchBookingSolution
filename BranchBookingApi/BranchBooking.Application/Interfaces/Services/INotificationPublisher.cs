using BranchBooking.Application.Models.Notifications;

namespace BranchBooking.Application.Interfaces.Services;

public interface INotificationPublisher
{
    Task PublishAsync(NotificationMessage message, CancellationToken cancellationToken = default);
}
