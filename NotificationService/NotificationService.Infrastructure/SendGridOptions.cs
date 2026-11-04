namespace NotificationService.Infrastructure;

public sealed class SendGridOptions
{
    public const string SectionName = "SendGrid";

    public bool Enabled { get; set; }
    public string ApiKey { get; set; } = string.Empty;
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = string.Empty;
}
