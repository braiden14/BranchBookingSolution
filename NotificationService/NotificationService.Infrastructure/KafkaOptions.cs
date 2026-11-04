using Confluent.Kafka;

namespace NotificationService.Infrastructure;

public sealed class KafkaOptions
{
    public const string SectionName = "Kafka";

    public string BootstrapServers { get; set; } = "localhost:9092";
    public string Topic { get; set; } = "notifications";
    public string GroupId { get; set; } = "notification-service";
    public AutoOffsetReset AutoOffsetReset { get; set; } = AutoOffsetReset.Earliest;
}
