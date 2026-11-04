using Microsoft.Extensions.Options;
using NotificationService.Application.Interfaces;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace NotificationService.Infrastructure;

public sealed class SendGridEmailSender(
    IOptions<SendGridOptions> options) : IEmailSender
{
    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        var settings = options.Value;
        if (string.IsNullOrWhiteSpace(settings.ApiKey) || string.IsNullOrWhiteSpace(settings.FromEmail))
        {
            throw new InvalidOperationException("SendGrid:ApiKey and SendGrid:FromEmail must be configured.");
        }

        var client = new SendGridClient(settings.ApiKey);
        var email = MailHelper.CreateSingleEmail(
            new EmailAddress(settings.FromEmail, settings.FromName),
            new EmailAddress(message.ToEmail, message.ToName),
            message.Subject,
            message.PlainTextBody,
            message.HtmlBody);

        var response = await client.SendEmailAsync(email, cancellationToken);
        if ((int)response.StatusCode >= 300)
        {
            var body = await response.Body.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"SendGrid rejected the email with status {(int)response.StatusCode}: {body}");
        }
    }
}
