// File: AuthService.Infrastructure/Messaging/Kafka/KafkaSettings.cs
// Purpose: Configuration settings for Kafka connection
// Layer: Infrastructure

namespace AuthService.Infrastructure.Messaging.Kafka;

public class KafkaSettings
{
    public string BootstrapServers { get; set; } = "localhost:9092";
    public string TopicPrefix { get; set; } = "auth";
    public string ConsumerGroupId { get; set; } = "auth-service-group";
    
    // 👇 ADDED: Future-proofing slots for Aiven Cloud Authentication
    public string? Username { get; set; }
    public string? Password { get; set; }
}
