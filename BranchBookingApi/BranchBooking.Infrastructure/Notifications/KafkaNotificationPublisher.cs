using System.Text.Json;
using BranchBooking.Application.Interfaces.Services;
using BranchBooking.Application.Models.Notifications;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BranchBooking.Infrastructure.Notifications;

public sealed class KafkaNotificationPublisher(
    IOptions<KafkaOptions> options,
    ILogger<KafkaNotificationPublisher> logger) : INotificationPublisher, IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly IProducer<Null, string> producer = new ProducerBuilder<Null, string>(new ProducerConfig
    {
        BootstrapServers = options.Value.BootstrapServers,
        Acks = Acks.All
    }).Build();

    public async Task PublishAsync(NotificationMessage message, CancellationToken cancellationToken = default)
    {
        try
        {
            var payload = JsonSerializer.Serialize(message, JsonOptions);
            await producer.ProduceAsync(
                options.Value.Topic,
                new Message<Null, string> { Value = payload },
                cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Could not publish notification {EventId} of type {EventType} to Kafka topic {Topic}",
                message.EventId,
                message.EventType,
                options.Value.Topic);
        }
    }

    public void Dispose() => producer.Dispose();
}
