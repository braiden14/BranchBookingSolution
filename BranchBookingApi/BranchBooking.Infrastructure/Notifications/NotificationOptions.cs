namespace BranchBooking.Infrastructure.Notifications;

public sealed class NotificationOptions
{
    public const string SectionName = "Notifications";

    public string InvitationUrlBase { get; set; } = "http://localhost:5174/accept-invitation";
}
