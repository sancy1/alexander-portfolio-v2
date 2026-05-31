
// File path: services/auth-service/AuthService.Infrastructure/Messaging/Kafka/KafkaProducer.cs

using System;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Confluent.Kafka;
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
            _logger.LogWarning("⚠️ Kafka configuration missing or incomplete. Kafka archiving is DISABLED.");
            _isEnabled = false;
            return;
        }

        try
        {
            var config = new ProducerConfig
            {
                BootstrapServers = _settings.BootstrapServers,
                SecurityProtocol = SecurityProtocol.SaslSsl,
                SaslMechanism = SaslMechanism.Plain, // Native Aiven standard PLAIN mechanism
                SaslUsername = _settings.Username,
                SaslPassword = _settings.Password,
                ClientId = "auth-service",
                EnableIdempotence = true, // Strict event order protection
                Acks = Acks.All, // Zero data loss guarantee
                MessageSendMaxRetries = 3,
                RetryBackoffMs = 1000,
                SocketTimeoutMs = 30000,
                MessageTimeoutMs = 30000,
                CompressionType = CompressionType.Snappy, // High memory execution efficiency
                BatchSize = 16384, // 16KB unmanaged buffer alignment
                LingerMs = 5 // Accumulation window to save IO ticks
            };

            _producer = new ProducerBuilder<string, string>(config).Build();
            _isEnabled = true;
            _logger.LogInformation("✅ Kafka producer initialized successfully for {BootstrapServers}", _settings.BootstrapServers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Critical Failure: Could not initialize native Kafka producer infrastructure");
            _isEnabled = false;
        }
    }

    public async Task ProduceAsync(string topic, string payload, CancellationToken cancellationToken = default)
    {
        if (!_isEnabled || _producer == null)
        {
            _logger.LogDebug("Skipping Kafka produce invocation (service disabled) for topic: {Topic}", topic);
            return;
        }

        try
        {
            // Enforcement: Verify payload stays strictly within target memory budget
            var payloadSize = Encoding.UTF8.GetByteCount(payload);
            if (payloadSize > _settings.MaxMessageBytes)
            {
                throw new InvalidOperationException($"Payload size ({payloadSize} bytes) exceeds explicit Kafka free-tier limits of {_settings.MaxMessageBytes} bytes. Claim check pattern violation.");
            }

            var kafkaMessage = new Message<string, string> 
            { 
                Key = Guid.NewGuid().ToString(), // Provides clear message key partitioning 
                Value = payload 
            };

            var result = await _producer.ProduceAsync(topic, kafkaMessage, cancellationToken);
            _logger.LogDebug("Message archived successfully to {Topic} at partition offset {Offset}", result.Topic, result.Offset);
        }
        catch (ProduceException<string, string> ex)
        {
            _logger.LogError(ex, "Failed to produce log entry to Kafka topic {Topic}. Reason: {Reason}", topic, ex.Error.Reason);
            throw; // Propagate up to allow outbox state retry handlers to track the failure
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unidentified infrastructure exception during Kafka write on topic {Topic}", topic);
            throw;
        }
    }

    public async Task ProduceAsync<T>(string topic, T payload, CancellationToken cancellationToken = default) where T : class
    {
        var json = JsonSerializer.Serialize(payload);
        await ProduceAsync(topic, json, cancellationToken);
    }

    public async Task ProduceAuditLogAsync<T>(T auditLog, CancellationToken cancellationToken = default) where T : class
    {
        var json = JsonSerializer.Serialize(auditLog);
        await ProduceAsync("security-audit-logs", json, cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        
        if (_producer != null)
        {
            try
            {
                // Synchronously flush outbound buffers to clear memory queues gracefully before tear down
                _producer.Flush(TimeSpan.FromSeconds(10));
                _producer.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception encountered during unmanaged Kafka producer resource disposal");
            }
        }
        
        _disposed = true;
        GC.SuppressFinalize(this);
        await Task.CompletedTask;
    }
}
