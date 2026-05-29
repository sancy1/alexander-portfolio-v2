// File: AuthService.Infrastructure/Messaging/Kafka/KafkaConsumer.cs
// Purpose: Consumes messages from Kafka topics for audit log processing with cloud circuit breakers
// Layer: Infrastructure

using System;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;

namespace AuthService.Infrastructure.Messaging.Kafka;

public interface IKafkaConsumer
{
    Task StartConsumingAsync(CancellationToken cancellationToken);
}

public class KafkaConsumer : BackgroundService, IKafkaConsumer
{
    private readonly IConsumer<string, string>? _consumer;
    private readonly KafkaSettings _settings;
    private readonly ILogger<KafkaConsumer> _logger;
    private readonly bool _isEnabled;

    public KafkaConsumer(IOptions<KafkaSettings> settings, ILogger<KafkaConsumer> logger)
    {
        _settings = settings.Value;
        _logger = logger;

        // 👇 FIX: Isolate and intercept unconfigured local variables instantly
        if (string.IsNullOrEmpty(_settings.BootstrapServers) || _settings.BootstrapServers.Contains("localhost"))
        {
            _logger.LogWarning("⚠️ Kafka consumer settings missing or set to localhost. Background worker thread is DEACTIVATED.");
            _isEnabled = false;
            return;
        }

        try
        {
            var config = new ConsumerConfig
            {
                BootstrapServers = _settings.BootstrapServers,
                GroupId = _settings.ConsumerGroupId,
                AutoOffsetReset = AutoOffsetReset.Earliest,
                EnableAutoCommit = false,
                
                // 👇 Future-proofing configurations for Aiven secure SASL handshakes
                SecurityProtocol = SecurityProtocol.SaslSsl,
                SaslMechanism = SaslMechanism.ScramSha256,
                SaslUsername = _settings.Username,
                SaslPassword = _settings.Password
            };

            _consumer = new ConsumerBuilder<string, string>(config).Build();
            _consumer.Subscribe($"{_settings.TopicPrefix}-audit-logs");
            _isEnabled = true;
            _logger.LogInformation("✅ Kafka background consumer service bound to topic: {Topic}-audit-logs", _settings.TopicPrefix);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to link native client connections to Kafka cloud clusters.");
            _isEnabled = false;
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // 👇 FIX: Instantly short-circuits the thread pool loop to protect your Render container resources
        if (!_isEnabled || _consumer == null)
        {
            _logger.LogDebug("Kafka consumer background processing loop skipped.");
            return;
        }

        // Run the background tracking loop asynchronously inside a dedicated task pool slot
        await Task.Run(() => StartConsumingAsync(stoppingToken), stoppingToken);
    }

    public async Task StartConsumingAsync(CancellationToken cancellationToken)
    {
        if (_consumer == null) return;

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var consumeResult = _consumer.Consume(cancellationToken);

                if (consumeResult != null)
                {
                    await ProcessMessage(consumeResult.Message.Value);
                    _consumer.Commit(consumeResult);
                }
            }
            catch (ConsumeException ex)
            {
                _logger.LogError("Kafka ingestion exception detected: {Reason}", ex.Error.Reason);
                
                // Slow down the processing speed on persistent cloud connectivity dropouts
                await Task.Delay(2000, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error inside Kafka background consumption cycle");
            }
        }

        try
        {
            _consumer.Close();
        }
        catch (Exception ex)
        {
            _logger.LogDebug("Kafka consumer socket channel closed down: {Msg}", ex.Message);
        }
    }

    private Task ProcessMessage(string message)
    {
        _logger.LogInformation("System Security Audit log stream intercept payload: {Message}", message);
        return Task.CompletedTask;
    }
}
