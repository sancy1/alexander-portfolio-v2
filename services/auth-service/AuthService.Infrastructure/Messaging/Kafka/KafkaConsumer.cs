// File: AuthService.Infrastructure/Messaging/Kafka/KafkaConsumer.cs

using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using StackExchange.Redis; // Assuming your working Redis layer relies on this or an interface equivalent
using AuthService.Application.Interfaces.Messaging;
using AuthService.Infrastructure.Persistence;

namespace AuthService.Infrastructure.Messaging.Kafka;

public sealed class KafkaConsumer : BackgroundService
{
    private readonly IConsumer<string, string>? _consumer;
    private readonly IKafkaProducer _kafkaProducer; // Reused Singleton to eliminate memory leak
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
            _logger.LogWarning("⚠️ Kafka consumer settings missing. Consumer is DEACTIVATED.");
            _isEnabled = false;
            return;
        }

        try
        {
            var consumerConfig = new ConsumerConfig
            {
                BootstrapServers = _settings.BootstrapServers,
                GroupId = _settings.ConsumerGroupId,
                AutoOffsetReset = AutoOffsetReset.Earliest,
                EnableAutoCommit = false, // Manual confirmation to eliminate double reads
                EnableAutoOffsetStore = false,
                SecurityProtocol = SecurityProtocol.SaslSsl,
                SaslMechanism = SaslMechanism.Plain,
                SaslUsername = _settings.Username,
                SaslPassword = _settings.Password,
                MaxPartitionFetchBytes = _settings.MaxPartitionFetchBytes, // Strict free tier restriction limits
                MaxPollIntervalMs = 300000, 
                SessionTimeoutMs = 45000,
                HeartbeatIntervalMs = 15000
            };

            _consumer = new ConsumerBuilder<string, string>(consumerConfig).Build();
            _consumer.Subscribe($"{_settings.TopicPrefix}-events");
            _isEnabled = true;
            _logger.LogInformation("✅ Kafka consumer subscribed to {Topic}-events. Error DLQ designated: {ErrorTopic}", _settings.TopicPrefix, _errorTopic);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Native Exception: Failed to initialize structural Kafka consumer thread");
            _isEnabled = false;
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_isEnabled || _consumer == null)
        {
            _logger.LogDebug("Kafka consumer execution skipped (Service layer inactive)");
            return;
        }

        // Move to long-running thread pool to isolate the continuous polling operation
        await Task.Factory.StartNew(
            () => ConsumeLoopAsync(stoppingToken), 
            stoppingToken, 
            TaskCreationOptions.LongRunning, 
            TaskScheduler.Default);
    }

    private async Task ConsumeLoopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Kafka background event polling listener active...");

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                // Synchronously poll internal fetch buffers safely matching cancellation bounds
                var consumeResult = _consumer.Consume(cancellationToken);
                if (consumeResult is null || consumeResult.IsPartitionEOF) continue;

                try
                {
                    // Create transient isolated scope per message to resolve database contexts
                    using var scope = _serviceProvider.CreateScope();
                    
                    // Resolve database and cache infrastructure dependencies within context bounds
                    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    var redisConnection = scope.ServiceProvider.GetService<IConnectionMultiplexer>(); 
                    
                    await ProcessClaimCheckMessageAsync(consumeResult.Message.Value, dbContext, redisConnection, cancellationToken);

                    // Message processed cleanly. Update index records safely.
                    _consumer.StoreOffset(consumeResult);
                    _consumer.Commit(consumeResult);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Processing failure encountered. Extracting reference frame to Dead Letter Topic.");
                    
                    var dlqPayload = new
                    {
                        OriginalPayload = consumeResult.Message.Value,
                        Topic = consumeResult.Topic,
                        Partition = consumeResult.Partition.Value,
                        Offset = consumeResult.Offset.Value,
                        FailureReason = ex.Message,
                        Timestamp = DateTime.UtcNow
                    };
                    
                    // Route using the pre-existing singleton producer to completely avoid unmanaged memory overhead
                    await _kafkaProducer.ProduceAsync(_errorTopic, dlqPayload, cancellationToken);
                    
                    // Explicitly advance offset to keep partition tracking lines flowing smoothly
                    _consumer.StoreOffset(consumeResult);
                    _consumer.Commit(consumeResult);
                }
            }
            catch (ConsumeException ex)
            {
                _logger.LogError(ex, "Kafka network transport exception: {Reason}", ex.Error.Reason);
                await Task.Delay(2000, cancellationToken); // Throttling backpressure layer
            }
            catch (OperationCanceledException)
            {
                break; // Graceful shutdown requested
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected infrastructure breakdown within consumer execution engine loop");
                await Task.Delay(5000, cancellationToken);
            }
        }

        // Close sockets and release unmanaged partition buffers cleanly inside final execution frame
        try
        {
            _consumer.Close();
            _consumer.Dispose();
            _logger.LogInformation("Kafka unmanaged consumer framework terminated gracefully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error encountered destroying native Kafka consumer layout resources");
        }
    }

    private async Task ProcessClaimCheckMessageAsync(string rawMessage, AppDbContext dbContext, IConnectionMultiplexer? redis, CancellationToken ct)
    {
        // 1. Enforce Claim Check Pattern: Read incoming lightweight JSON payload
        using var jsonDoc = JsonDocument.Parse(rawMessage);
        if (!jsonDoc.RootElement.TryGetProperty("Id", out var idElement))
        {
            throw new ArgumentException("Invalid event signature. Missing core unique tracking validation reference 'Id'.");
        }

        var entityId = idElement.GetString();
        if (string.IsNullOrWhiteSpace(entityId)) return;

        _logger.LogInformation("Processing incoming reference event logic for tracking key: {Id}", entityId);

        // 2. Intercept with Redis Layer first to achieve full Database Protection
        if (redis is not Birdge && redis != null)
        {
            var cacheKey = $"{_settings.TopicPrefix}:references:{entityId}";
            var cacheDb = redis.GetDatabase();
            
            var cachedData = await cacheDb.StringGetAsync(cacheKey);
            if (!cachedData.IsNullOrEmpty)
            {
                _logger.LogInformation("🚀 Redis Cache Hit for reference ID: {Id}. Database bypassed cleanly.", entityId);
                // Execute business logic with fast cached data frame here
                return;
            }

            _logger.LogWarning("⚠️ Redis Cache Miss for reference ID: {Id}. Executing fallback index database read lookup.", entityId);
        }

        // 3. Last Resort Fallback: Hit DB via fast Primary Key index match
        if (Guid.TryParse(entityId, out var parsedGuid))
        {
            var dataRecordExists = await dbContext.SocialUsers.FindAsync(new object[] { parsedGuid }, ct);
            
            if (dataRecordExists != null && redis != null)
            {
                // Write back to memory layer with an explicit TTL to prevent future database execution spikes
                var cacheKey = $"{_settings.TopicPrefix}:references:{entityId}";
                var cacheDb = redis.GetDatabase();
                await cacheDb.StringSetAsync(cacheKey, JsonSerializer.Serialize(dataRecordExists), TimeSpan.FromHours(1));
                _logger.LogInformation("State reference cache synchronized for Key {Id}", entityId);
            }
        }
    }
}
