using NSubstitute;
using NotificationService.Application;
using NotificationService.Application.Interfaces;
using NotificationService.Core;

namespace NotificationService.Tests;

public sealed class NotificationHandlerTests
{
    [Test]
    public async Task RoutesBookingConfirmationToRegisteredHandler()
    {
        var handler = Substitute.For<INotificationHandler>();
        handler.CanHandle(NotificationEventType.BookingConfirmation).Returns(true);
        var sut = new NotificationHandler([handler]);
        var message = new NotificationMessage("event-1", NotificationEventType.BookingConfirmation, "customer@example.com", "Customer", new Dictionary<string, string>());

        await sut.HandleAsync(message);

        await handler.Received(1).HandleAsync(message, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task BookingConfirmationBuildsExpectedEmail()
    {
        var sender = Substitute.For<IEmailSender>();
        var sut = new BookingConfirmationHandler(sender);
        var message = new NotificationMessage("event-1", NotificationEventType.BookingConfirmation, "customer@example.com", "Customer", new Dictionary<string, string>
        {
            ["bookingReference"] = "BK-100",
            ["serviceName"] = "Consultation",
            ["startTime"] = "10:00"
        });

        await sut.HandleAsync(message);

        await sender.Received(1).SendAsync(
            Arg.Is<EmailMessage>(email => email.Subject.Contains("BK-100") && email.HtmlBody.Contains("Consultation")),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task StaffInvitationBuildsInvitationEmail()
    {
        var sender = Substitute.For<IEmailSender>();
        var sut = new StaffInvitationHandler(sender);
        var message = new NotificationMessage("event-2", NotificationEventType.StaffInvitation, "staff@example.com", "Staff", new Dictionary<string, string>
        {
            ["branchName"] = "Central Branch",
            ["invitationLink"] = "https://example.com/invite"
        });

        await sut.HandleAsync(message);

        await sender.Received(1).SendAsync(
            Arg.Is<EmailMessage>(email => email.Subject.Contains("Central Branch") && email.PlainTextBody.Contains("https://example.com/invite")),
            Arg.Any<CancellationToken>());
    }
}
