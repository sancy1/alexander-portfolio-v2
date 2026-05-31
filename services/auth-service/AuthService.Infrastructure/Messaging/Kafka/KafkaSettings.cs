
// services/auth-service/AuthService.Infrastructure/Messaging/Kafka/KafkaSettings.cs

namespace AuthService.Infrastructure.Messaging.Kafka;

public class KafkaSettings
{
    public string BootstrapServers { get; set; } = "localhost:9092";
    public string TopicPrefix { get; set; } = "auth";
    public string ConsumerGroupId { get; set; } = "auth-service-group";
    
    // Aiven Kafka Cloud Authentication
    public string? Username { get; set; }
    public string? Password { get; set; }
    
    // Cloud Security Configuration (SASL_SSL / SCRAM-SHA-256 for Aiven)
    public string SecurityProtocol { get; set; } = "SaslSsl";
    public string SaslMechanism { get; set; } = "ScramSha256";
    
    // Free Tier Heap & Protection Settings
    public int MaxMessageBytes { get; set; } = 1048576;      // 1MB production max message limit
    public int MaxPollRecords { get; set; } = 10;            // Keeps micro-batches lean
    public int FetchMaxBytes { get; set; } = 5242880;        // 5MB aggregate fetch maximum
    public int MaxPartitionFetchBytes { get; set; } = 262144; // 256KB strict limit per partition buffer
    
    // Dead Letter Management
    public string ErrorTopicSuffix { get; set; } = "-error";
    
    // Safety runtime toggle
    public bool IsConfigured => 
        !string.IsNullOrEmpty(BootstrapServers) && 
        !BootstrapServers.Contains("localhost") &&
        !string.IsNullOrEmpty(Username) &&
        !string.IsNullOrEmpty(Password);
}
