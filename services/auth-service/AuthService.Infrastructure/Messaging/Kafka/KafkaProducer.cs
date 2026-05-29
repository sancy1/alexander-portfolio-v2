// File: services/auth-service/AuthService.Infrastructure/Messaging/Kafka/KafkaProducer.cs
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Confluent.Kafka;

namespace AuthService.Infrastructure.Messaging.Kafka;

public interface IKafkaProducer
{
    Task ProduceAsync(string topic, string payload);
    Task ProduceAuditLogAsync(object auditLog);
}

public class KafkaProducer : IKafkaProducer
{
    private readonly IProducer<string, string>? _producer;
    private readonly ILogger<KafkaProducer> _logger;
    private readonly bool _isEnabled;

    public KafkaProducer(ILogger<KafkaProducer> logger)
    {
        _logger = logger;
        
        // 👇 CHECK: Extract your Aiven Kafka variables
        var bootstrapServers = Environment.GetEnvironmentVariable("KAFKA_BOOTSTRAP_SERVERS");

        if (string.IsNullOrEmpty(bootstrapServers) || bootstrapServers.Contains("localhost"))
        {
            _logger.LogWarning("⚠️ Kafka configuration missing or set to localhost. Kafka archiving is DISABLED for this session.");
            _isEnabled = false;
            return;
        }

        try
        {
            var config = new ProducerConfig
            {
                BootstrapServers = bootstrapServers,
                SecurityProtocol = SecurityProtocol.SaslSsl,
                SaslMechanism = SaslMechanism.ScramSha256,
                SaslUsername = Environment.GetEnvironmentVariable("KAFKA_USERNAME"),
                SaslPassword = Environment.GetEnvironmentVariable("KAFKA_PASSWORD")
            };

            _producer = new ProducerBuilder<string, string>(config).Build();
            _isEnabled = true;
            _logger.LogInformation("✅ Confluent Kafka cloud producer initialized successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to initialize Kafka cloud connection wrapper configurations.");
            _isEnabled = false;
        }
    }

    public async Task ProduceAsync(string topic, string payload)
    {
        if (!_isEnabled || _producer == null)
        {
            _logger.LogDebug("Skipping Kafka produce token for topic {Topic} (Service disabled)", topic);
            return;
        }

        try
        {
            await _producer.ProduceAsync(topic, new Message<string, string> { Value = payload });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to stream outbox transaction history to Kafka topic {Topic}", topic);
        }
    }

    public async Task ProduceAuditLogAsync(object auditLog)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(auditLog);
        await ProduceAsync("security-audit-logs", json);
    }
}
