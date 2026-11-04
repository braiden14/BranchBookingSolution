using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NotificationService.Application;
using NotificationService.Application.Interfaces;

namespace NotificationService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddNotificationInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<KafkaOptions>(configuration.GetSection(KafkaOptions.SectionName));
        services.Configure<SendGridOptions>(configuration.GetSection(SendGridOptions.SectionName));
        services.AddSingleton<IEmailSender>(serviceProvider =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<SendGridOptions>>().Value;
            return options.Enabled
                ? new SendGridEmailSender(serviceProvider.GetRequiredService<IOptions<SendGridOptions>>())
                : new LoggingEmailSender(serviceProvider.GetRequiredService<ILogger<LoggingEmailSender>>());
        });
        services.AddSingleton<INotificationHandler, BookingConfirmationHandler>();
        services.AddSingleton<INotificationHandler, StaffInvitationHandler>();
        services.AddSingleton<NotificationHandler>();
        services.AddHostedService<KafkaNotificationConsumer>();
        return services;
    }
}
