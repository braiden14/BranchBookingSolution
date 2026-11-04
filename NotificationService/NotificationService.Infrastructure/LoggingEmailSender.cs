using Microsoft.Extensions.Logging;
using NotificationService.Application.Interfaces;

namespace NotificationService.Infrastructure;

public sealed class LoggingEmailSender(ILogger<LoggingEmailSender> logger) : IEmailSender
{
    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "SendGrid is disabled. Email was not sent. Recipient: {RecipientEmail} ({RecipientName}); Subject: {Subject}; Plain text body: {PlainTextBody}; HTML body: {HtmlBody}",
            message.ToEmail,
            message.ToName,
            message.Subject,
            message.PlainTextBody,
            message.HtmlBody);

        return Task.CompletedTask;
    }
}
