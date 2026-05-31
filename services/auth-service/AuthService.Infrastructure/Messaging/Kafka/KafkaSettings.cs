// File: AuthService.Infrastructure/Messaging/Kafka/KafkaSettings.cs
namespace AuthService.Infrastructure.Messaging.Kafka;

public class KafkaSettings
{
    public string BootstrapServers { get; set; } = "localhost:9092";
    public string TopicPrefix { get; set; } = "auth";
    public string ConsumerGroupId { get; set; } = "auth-service-group";

    // Aiven Kafka SASL Authentication
    public string? Username { get; set; }
    public string? Password { get; set; }

    // Security — loaded from env, defaults to Aiven SASL standard
    public string SecurityProtocol { get; set; } = "SaslSsl";
    public string SaslMechanism { get; set; } = "ScramSha256";

    // Free Tier Memory Protection
    public int MaxMessageBytes { get; set; } = 1048576;
    public int MaxPollRecords { get; set; } = 10;
    public int FetchMaxBytes { get; set; } = 5242880;
    public int MaxPartitionFetchBytes { get; set; } = 262144;

    // Dead Letter Queue
    public string ErrorTopicSuffix { get; set; } = "-error";

    public bool IsConfigured =>
        !string.IsNullOrEmpty(BootstrapServers) &&
        !BootstrapServers.Contains("localhost") &&
        !string.IsNullOrEmpty(Username) &&
        !string.IsNullOrEmpty(Password);
}