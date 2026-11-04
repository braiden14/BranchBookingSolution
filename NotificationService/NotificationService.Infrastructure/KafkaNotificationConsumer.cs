using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NotificationService.Application;
using NotificationService.Core;

namespace NotificationService.Infrastructure;

public sealed class KafkaNotificationConsumer(
    IOptions<KafkaOptions> options,
    NotificationHandler handler,
    ILogger<KafkaNotificationConsumer> logger) : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return Task.Run(() => ConsumeAsync(stoppingToken), stoppingToken);
    }

    private async Task ConsumeAsync(CancellationToken stoppingToken)
    {
        var settings = options.Value;
        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = settings.BootstrapServers,
            GroupId = settings.GroupId,
            AutoOffsetReset = settings.AutoOffsetReset,
            EnableAutoCommit = false,
            AllowAutoCreateTopics = false
        };

        using var consumer = new ConsumerBuilder<Ignore, string>(consumerConfig).Build();
        consumer.Subscribe(settings.Topic);
        logger.LogInformation("Consuming notifications from Kafka topic {Topic}", settings.Topic);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var result = consumer.Consume(stoppingToken);
                    var message = JsonSerializer.Deserialize<NotificationMessage>(result.Message.Value, JsonOptions)
                        ?? throw new JsonException("Kafka message contained an empty notification payload.");

                    await handler.HandleAsync(message, stoppingToken);
                    consumer.Commit(result);
                    logger.LogInformation("Processed notification {EventId} of type {EventType}", message.EventId, message.EventType);
                }
                catch (ConsumeException exception)
                {
                    logger.LogError(exception, "Kafka consumption failed: {Reason}", exception.Error.Reason);
                }
                catch (JsonException exception)
                {
                    logger.LogError(exception, "Ignoring malformed notification message.");
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception exception)
                {
                    logger.LogError(exception, "Notification processing failed; the Kafka offset was not committed.");
                }
            }
        }
        finally
        {
            consumer.Close();
        }
    }
}
