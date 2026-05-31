// File: AuthService.Infrastructure/Messaging/Kafka/KafkaConsumer.cs
using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using AuthService.Application.Interfaces.Messaging;
using AuthService.Infrastructure.Caching;

namespace AuthService.Infrastructure.Messaging.Kafka;

public sealed class KafkaConsumer : BackgroundService
{
    private readonly IConsumer<string, string>? _consumer;
    private readonly IKafkaProducer _kafkaProducer;
    private readonly IServiceProvider _serviceProvider;
    private readonly KafkaSettings _settings;
    private readonly ILogger<KafkaConsumer> _logger;
    private readonly bool _isEnabled;
    private readonly string _errorTopic;

    public KafkaConsumer(
        IOptions<KafkaSettings> settings,
        IKafkaProducer kafkaProducer,
        IServiceProvider serviceProvider,
        ILogger<KafkaConsumer> logger)
    {
        _settings = settings.Value;
        _kafkaProducer = kafkaProducer;
        _serviceProvider = serviceProvider;
        _logger = logger;
        _errorTopic = $"{_settings.TopicPrefix}{_settings.ErrorTopicSuffix}";

        if (!_settings.IsConfigured)
        {
            _logger.LogWarning("⚠️ Kafka consumer settings missing. Consumer DEACTIVATED.");
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
                _                                => SaslMechanism.ScramSha256
            };

            var consumerConfig = new ConsumerConfig
            {
                BootstrapServers = _settings.BootstrapServers,
                GroupId = _settings.ConsumerGroupId,
                AutoOffsetReset = AutoOffsetReset.Earliest,
                EnableAutoCommit = false,
                EnableAutoOffsetStore = false,
                SecurityProtocol = SecurityProtocol.SaslSsl,
                SaslMechanism = saslMechanism,
                SaslUsername = _settings.Username,
                SaslPassword = _settings.Password,
                MaxPartitionFetchBytes = _settings.MaxPartitionFetchBytes,
                MaxPollIntervalMs = 300000,
                SessionTimeoutMs = 45000,
                HeartbeatIntervalMs = 15000
            };

            _consumer = new ConsumerBuilder<string, string>(consumerConfig).Build();
            _consumer.Subscribe($"{_settings.TopicPrefix}-events");
            _isEnabled = true;

            _logger.LogInformation(
                "✅ Kafka consumer subscribed to {Topic}-events | DLQ: {ErrorTopic} | Mechanism: {Mechanism}",
                _settings.TopicPrefix, _errorTopic, saslMechanism);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to initialize Kafka consumer");
            _isEnabled = false;
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_isEnabled || _consumer == null)
        {
            _logger.LogDebug("Kafka consumer skipped (inactive)");
            return;
        }

        await Task.Factory.StartNew(
            () => ConsumeLoopAsync(stoppingToken),
            stoppingToken,
            TaskCreationOptions.LongRunning,
            TaskScheduler.Default);
    }

    private async Task ConsumeLoopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Kafka consumer polling loop started...");

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var consumeResult = _consumer.Consume(cancellationToken);
                if (consumeResult is null || consumeResult.IsPartitionEOF) continue;

                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var redisCache = scope.ServiceProvider.GetService<IRedisCacheService>();

                    await ProcessMessageAsync(
                        consumeResult.Message.Value,
                        redisCache,
                        cancellationToken);

                    _consumer.StoreOffset(consumeResult);
                    _consumer.Commit(consumeResult);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Message processing failed. Routing to DLQ.");

                    await _kafkaProducer.ProduceAsync(_errorTopic, new
                    {
                        OriginalPayload = consumeResult.Message.Value,
                        Topic = consumeResult.Topic,
                        Partition = consumeResult.Partition.Value,
                        Offset = consumeResult.Offset.Value,
                        FailureReason = ex.Message,
                        Timestamp = DateTime.UtcNow
                    }, cancellationToken);

                    _consumer.StoreOffset(consumeResult);
                    _consumer.Commit(consumeResult);
                }
            }
            catch (ConsumeException ex)
            {
                _logger.LogError(ex, "Kafka consume error: {Reason}", ex.Error.Reason);
                await Task.Delay(2000, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in Kafka consumer loop");
                await Task.Delay(5000, cancellationToken);
            }
        }

        try
        {
            _consumer.Close();
            _consumer.Dispose();
            _logger.LogInformation("Kafka consumer shut down gracefully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disposing Kafka consumer");
        }
    }

    private async Task ProcessMessageAsync(
        string rawMessage,
        IRedisCacheService? redisCache,
        CancellationToken ct)
    {
        using var jsonDoc = JsonDocument.Parse(rawMessage);

        if (!jsonDoc.RootElement.TryGetProperty("Id", out var idElement))
        {
            _logger.LogWarning("Kafka message missing 'Id' field — skipping");
            return;
        }

        var entityId = idElement.GetString();
        if (string.IsNullOrWhiteSpace(entityId)) return;

        _logger.LogInformation("Processing Kafka event for Id: {Id}", entityId);

        // Check Redis cache first
        if (redisCache != null)
        {
            var cacheKey = $"{_settings.TopicPrefix}:references:{entityId}";
            var cached = await redisCache.GetAsync<string>(cacheKey);
            if (cached != null)
            {
                _logger.LogInformation("Cache hit for {Id} — database bypassed", entityId);
                return;
            }
        }

        // Future: add domain-specific processing here per event type
        _logger.LogInformation("Kafka event {Id} processed successfully", entityId);

        await Task.CompletedTask;
    }
}