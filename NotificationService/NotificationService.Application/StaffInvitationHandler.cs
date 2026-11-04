using NotificationService.Application.Interfaces;
using NotificationService.Core;

namespace NotificationService.Application;

public sealed class StaffInvitationHandler(IEmailSender emailSender) : INotificationHandler
{
    public bool CanHandle(NotificationEventType eventType) => eventType == NotificationEventType.StaffInvitation;

    public Task HandleAsync(NotificationMessage message, CancellationToken cancellationToken = default)
    {
        var branchName = message.Data.GetValueOrDefault("branchName", "your branch");
        var invitationLink = message.Data.GetValueOrDefault("invitationLink", string.Empty);

        return emailSender.SendAsync(
            new EmailMessage(
                message.RecipientEmail,
                message.RecipientName,
                $"You have been invited to join {branchName}",
                $"<p>Hi {message.RecipientName},</p><p>You have been invited to join <strong>{branchName}</strong> as a staff member.</p><p><a href=\"{invitationLink}\">Accept your invitation</a></p>",
                $"Hi {message.RecipientName},\n\nYou have been invited to join {branchName} as a staff member.\nAccept your invitation: {invitationLink}"),
            cancellationToken);
    }
}
