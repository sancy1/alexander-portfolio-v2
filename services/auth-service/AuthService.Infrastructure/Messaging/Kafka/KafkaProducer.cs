// File: AuthService.Infrastructure/Messaging/Kafka/KafkaProducer.cs
using System.Text;
using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using AuthService.Application.Interfaces.Messaging;

namespace AuthService.Infrastructure.Messaging.Kafka;

public sealed class KafkaProducer : IKafkaProducer, IAsyncDisposable
{
    private readonly IProducer<string, string>? _producer;
    private readonly KafkaSettings _settings;
    private readonly ILogger<KafkaProducer> _logger;
    private readonly bool _isEnabled;
    private bool _disposed;

    public KafkaProducer(IOptions<KafkaSettings> settings, ILogger<KafkaProducer> logger)
    {
        _settings = settings.Value;
        _logger = logger;

        if (!_settings.IsConfigured)
        {
            _logger.LogWarning("⚠️ Kafka configuration missing. Producer DISABLED.");
            _isEnabled = false;
            return;
        }

        try
        {
            // Parse mechanism from settings — never hardcoded
            var saslMechanism = _settings.SaslMechanism.ToLower() switch
            {
                "scramsha256" or "scram-sha-256" => SaslMechanism.ScramSha256,
                "scramsha512" or "scram-sha-512" => SaslMechanism.ScramSha512,
                "plain"                          => SaslMechanism.Plain,
                _                                => SaslMechanism.ScramSha256 // Aiven default
            };

            var config = new ProducerConfig
            {
                BootstrapServers = _settings.BootstrapServers,
                SecurityProtocol = SecurityProtocol.SaslSsl,
                SaslMechanism = saslMechanism,
                SaslUsername = _settings.Username,
                SaslPassword = _settings.Password,
                SslCaLocation = "/app/ca.pem"
                ClientId = "auth-service",
                EnableIdempotence = true,
                Acks = Acks.All,
                MessageSendMaxRetries = 3,
                RetryBackoffMs = 1000,
                SocketTimeoutMs = 30000,
                MessageTimeoutMs = 30000,
                CompressionType = CompressionType.Snappy,
                BatchSize = 16384,
                LingerMs = 5
            };

            _producer = new ProducerBuilder<string, string>(config).Build();
            _isEnabled = true;
            _logger.LogInformation(
                "✅ Kafka producer initialized: {BootstrapServers} | Mechanism: {Mechanism}",
                _settings.BootstrapServers, saslMechanism);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to initialize Kafka producer");
            _isEnabled = false;
        }
    }

    public async Task ProduceAsync(string topic, string payload, CancellationToken cancellationToken = default)
    {
        if (!_isEnabled || _producer == null)
        {
            _logger.LogDebug("Kafka produce skipped (disabled) for topic: {Topic}", topic);
            return;
        }

        try
        {
            var payloadSize = Encoding.UTF8.GetByteCount(payload);
            if (payloadSize > _settings.MaxMessageBytes)
                throw new InvalidOperationException(
                    $"Payload {payloadSize} bytes exceeds limit {_settings.MaxMessageBytes} bytes.");

            var result = await _producer.ProduceAsync(
                topic,
                new Message<string, string>
                {
                    Key = Guid.NewGuid().ToString(),
                    Value = payload
                },
                cancellationToken);

            _logger.LogDebug(
                "Kafka message archived to {Topic} at offset {Offset}",
                result.Topic, result.Offset);
        }
        catch (ProduceException<string, string> ex)
        {
            _logger.LogError(ex, "Kafka produce failed for topic {Topic}: {Reason}", topic, ex.Error.Reason);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error producing to Kafka topic {Topic}", topic);
            throw;
        }
    }

    public async Task ProduceAsync<T>(string topic, T payload, CancellationToken cancellationToken = default)
        where T : class
    {
        await ProduceAsync(topic, JsonSerializer.Serialize(payload), cancellationToken);
    }

    public async Task ProduceAuditLogAsync<T>(T auditLog, CancellationToken cancellationToken = default)
        where T : class
    {
        await ProduceAsync("security-audit-logs", JsonSerializer.Serialize(auditLog), cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        if (_producer != null)
        {
            try
            {
                _producer.Flush(TimeSpan.FromSeconds(10));
                _producer.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disposing Kafka producer");
            }
        }
        _disposed = true;
        GC.SuppressFinalize(this);
        await Task.CompletedTask;
    }
}