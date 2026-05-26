// File: AuthService.Infrastructure/Messaging/Kafka/KafkaProducer.cs
// Purpose: Produces messages to Kafka topics for audit logging
// Layer: Infrastructure

using System.Text;
using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.Options;

namespace AuthService.Infrastructure.Messaging.Kafka;

public interface IKafkaProducer
{
    Task ProduceAsync<T>(string topic, T message);
    Task ProduceAuditLogAsync<T>(T message);
}

public class KafkaProducer : IKafkaProducer
{
    private readonly IProducer<string, string> _producer;
    private readonly KafkaSettings _settings;

    public KafkaProducer(IOptions<KafkaSettings> settings)
    {
        _settings = settings.Value;

        var config = new ProducerConfig
        {
            BootstrapServers = _settings.BootstrapServers,
            ClientId = "auth-service",
            Acks = Acks.All,
            EnableIdempotence = true,
            CompressionType = CompressionType.Snappy,
            MessageSendMaxRetries = 3,
            RetryBackoffMs = 100
        };

        _producer = new ProducerBuilder<string, string>(config).Build();
    }

    public async Task ProduceAsync<T>(string topic, T message)
    {
        var json = JsonSerializer.Serialize(message);
        var result = await _producer.ProduceAsync(topic, new Message<string, string>
        {
            Key = Guid.NewGuid().ToString(),
            Value = json,
            Headers = new Headers
            {
                { "timestamp", BitConverter.GetBytes(DateTime.UtcNow.Ticks) },
                { "service", Encoding.UTF8.GetBytes("auth-service") },
                { "message_type", Encoding.UTF8.GetBytes(typeof(T).Name) }
            }
        });
    }

    public async Task ProduceAuditLogAsync<T>(T message)
    {
        var topic = $"{_settings.TopicPrefix}-audit-logs";
        await ProduceAsync(topic, message);
    }
}