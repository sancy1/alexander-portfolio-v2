// // File: AuthService.Infrastructure/Services/OutboxProcessorService.cs
// using Microsoft.Extensions.Logging;
// using Microsoft.Extensions.Hosting;
// using Microsoft.Extensions.DependencyInjection;
// using AuthService.Application.Interfaces.Persistence;
// using AuthService.Application.Interfaces.Messaging;
// using AuthService.Infrastructure.Messaging.Kafka;
// using AuthService.Domain.Entities;
// using System.Text.Json;

// namespace AuthService.Infrastructure.Services;

// public class OutboxProcessorService : BackgroundService, IOutboxProcessorService
// {
//     private readonly IServiceProvider _serviceProvider;
//     private readonly ILogger<OutboxProcessorService> _logger;
//     private readonly int _batchSize = 10;        // reduced from 20
//     private readonly int _sleepSeconds = 30;     // increased from 5 — protects connection pool
//     private readonly int _maxRetryCount = 3;

//     public OutboxProcessorService(
//         IServiceProvider serviceProvider,
//         ILogger<OutboxProcessorService> logger)
//     {
//         _serviceProvider = serviceProvider;
//         _logger = logger;
//     }

//     protected override async Task ExecuteAsync(CancellationToken stoppingToken)
//     {
//         _logger.LogInformation("🚀 Outbox Processor Service started...");

//         // Initial delay — let the app fully start before first poll
//         await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);

//         while (!stoppingToken.IsCancellationRequested)
//         {
//             try
//             {
//                 await ProcessBatchAsync(stoppingToken);
//             }
//             catch (Exception ex)
//             {
//                 _logger.LogError(ex, "Error in outbox processor loop");
//             }

//             await Task.Delay(TimeSpan.FromSeconds(_sleepSeconds), stoppingToken);
//         }
//     }

//     private async Task ProcessBatchAsync(CancellationToken stoppingToken)
//     {
//         using var scope = _serviceProvider.CreateScope();
//         var outboxRepository = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();
//         var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
//         var rabbitMQPublisher = scope.ServiceProvider.GetService<IMessagePublisher>();
//         var kafkaProducer = scope.ServiceProvider.GetService<IKafkaProducer>();

//         var pendingMessages = await outboxRepository.GetUnprocessedMessagesAsync(_batchSize);

//         if (!pendingMessages.Any())
//             return;

//         _logger.LogInformation("Processing {Count} pending outbox messages", pendingMessages.Count);

//         foreach (var message in pendingMessages)
//         {
//             await ProcessMessageAsync(message, rabbitMQPublisher, kafkaProducer, outboxRepository);
//         }

//         await unitOfWork.SaveChangesAsync(stoppingToken);
//     }

//     private async Task ProcessMessageAsync(
//         OutboxMessage message,
//         IMessagePublisher? rabbitMQPublisher,
//         IKafkaProducer? kafkaProducer,
//         IOutboxRepository outboxRepository)
//     {
//         try
//         {
//             var parsedPayload = JsonDocument.Parse(message.Payload).RootElement;
//             var brokerTarget = message.Broker?.ToLower() ?? "rabbitmq";

//             var targetRabbitMQ = brokerTarget is "all" or "both" or "rabbitmq";
//             var targetKafka = brokerTarget is "all" or "both" or "kafka";

//             if (targetRabbitMQ && rabbitMQPublisher != null)
//                 await rabbitMQPublisher.PublishAsync(message.RoutingKey, parsedPayload);

//             if (targetKafka && kafkaProducer != null)
//                 await kafkaProducer.ProduceAsync(message.RoutingKey, message.Payload);

//             message.ProcessedAt = DateTime.UtcNow;
//             message.Error = null;
//             await outboxRepository.UpdateAsync(message);
//         }
//         catch (Exception ex)
//         {
//             message.RetryCount += 1;
//             message.Error = ex.Message;

//             if (message.RetryCount >= _maxRetryCount)
//             {
//                 _logger.LogError(ex,
//                     "🚨 Outbox message {Id} dead-lettered after {Count} attempts",
//                     message.Id, _maxRetryCount);
//                 message.ProcessedAt = DateTime.UtcNow;
//             }
//             else
//             {
//                 _logger.LogWarning(ex,
//                     "Outbox retry {Count}/{Max} for message {Id}",
//                     message.RetryCount, _maxRetryCount, message.Id);
//             }

//             await outboxRepository.UpdateAsync(message);
//         }
//     }

//     public async Task<int> ProcessPendingMessagesAsync(int maxMessages = 10)
//     {
//         using var scope = _serviceProvider.CreateScope();
//         var outboxRepository = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();
//         var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
//         var rabbitMQPublisher = scope.ServiceProvider.GetService<IMessagePublisher>();
//         var kafkaProducer = scope.ServiceProvider.GetService<IKafkaProducer>();

//         var messages = await outboxRepository.GetUnprocessedMessagesAsync(maxMessages);
//         foreach (var message in messages)
//             await ProcessMessageAsync(message, rabbitMQPublisher, kafkaProducer, outboxRepository);

//         await unitOfWork.SaveChangesAsync();
//         return messages.Count;
//     }

//     public async Task<int> GetPendingCountAsync()
//     {
//         using var scope = _serviceProvider.CreateScope();
//         var outboxRepository = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();
//         return await outboxRepository.GetPendingCountAsync();
//     }

//     public async Task CleanupProcessedMessagesAsync(int daysToKeep = 7)
//     {
//         using var scope = _serviceProvider.CreateScope();
//         var outboxRepository = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();
//         var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
//         await outboxRepository.CleanupProcessedMessagesAsync(daysToKeep);
//         await unitOfWork.SaveChangesAsync();
//     }
// }




























// File: services/auth-service/AuthService.Infrastructure/Services/OutboxProcessorService.cs

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using AuthService.Application.Interfaces.Persistence;
using AuthService.Application.Interfaces.Messaging;
using AuthService.Domain.Entities;
using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace AuthService.Infrastructure.Services;

/// <summary>
/// Background worker process responsible for the safe transactional relay of outbound 
/// event records from PostgreSQL to Kafka and RabbitMQ brokers.
/// </summary>
public class OutboxProcessorService : BackgroundService, IOutboxProcessorService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OutboxProcessorService> _logger;
    private readonly int _batchSize = 10;        
    private readonly int _sleepSeconds = 30;     
    private readonly int _maxRetryCount = 3;

    public OutboxProcessorService(
        IServiceProvider serviceProvider,
        ILogger<OutboxProcessorService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("🚀 Outbox Processor Service started successfully.");

        // Initial grace period to allow main framework hosts to complete cold boot sequences
        await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessBatchAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Critical failure caught inside the Outbox Processor loop execution frame.");
            }

            await Task.Delay(TimeSpan.FromSeconds(_sleepSeconds), stoppingToken);
        }
    }

    private async Task ProcessBatchAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var outboxRepository = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var rabbitMQPublisher = scope.ServiceProvider.GetService<IMessagePublisher>();
        var kafkaProducer = scope.ServiceProvider.GetService<IKafkaProducer>();

        var pendingMessages = await outboxRepository.GetUnprocessedMessagesAsync(_batchSize);

        if (pendingMessages == null || !pendingMessages.Any())
            return;

        _logger.LogInformation("Processing {Count} pending transactional outbox messages.", pendingMessages.Count);

        foreach (var message in pendingMessages)
        {
            if (stoppingToken.IsCancellationRequested) break;
            
            await ProcessMessageAsync(message, rabbitMQPublisher, kafkaProducer, outboxRepository);
        }

        // Secure state synchronization lock across the database context provider
        await unitOfWork.SaveChangesAsync(stoppingToken);
    }

    private async Task ProcessMessageAsync(
        OutboxMessage message,
        IMessagePublisher? rabbitMQPublisher,
        IKafkaProducer? kafkaProducer,
        IOutboxRepository outboxRepository)
    {
        try
        {
            var parsedPayload = JsonDocument.Parse(message.Payload).RootElement;
            var brokerTarget = message.Broker?.ToLowerInvariant() ?? "rabbitmq";

            var targetRabbitMQ = brokerTarget is "all" or "both" or "rabbitmq";
            var targetKafka = brokerTarget is "all" or "both" or "kafka";

            // 1. Dispatch execution path targeting real-time RabbitMQ channels
            if (targetRabbitMQ && rabbitMQPublisher != null)
            {
                await rabbitMQPublisher.PublishAsync(message.RoutingKey, parsedPayload);
            }

            // 2. Dispatch execution path targeting permanent chronological Kafka streams
            if (targetKafka && kafkaProducer != null)
            {
                // 🧠 Optimization Fix: Map topic tracking parameters directly to message.EventType (auth-events)
                // instead of routing keys (admin.loggedin) to ensure correct Aiven partition matches.
                await kafkaProducer.ProduceAsync(message.EventType, message.Payload);
            }

            // Mark record as safely dispatched on success
            message.ProcessedAt = DateTime.UtcNow;
            message.Error = null;
            await outboxRepository.UpdateAsync(message);
        }
        catch (Exception ex)
        {
            message.RetryCount += 1;
            message.Error = ex.Message;

            if (message.RetryCount >= _maxRetryCount)
            {
                _logger.LogError(ex,
                    "🚨 Outbox message {Id} dead-lettered permanently after reaching max {Count} attempts. Error: {Error}",
                    message.Id, _maxRetryCount, ex.Message);
                
                // Advance the time block so dead messages do not jam active processing pipes
                message.ProcessedAt = DateTime.UtcNow;
            }
            else
            {
                _logger.LogWarning(ex,
                    "Outbox retry step execution {Count}/{Max} recorded for tracking event message ID: {Id}",
                    message.RetryCount, _maxRetryCount, message.Id);
            }

            await outboxRepository.UpdateAsync(message);
        }
    }

    public async Task<int> ProcessPendingMessagesAsync(int maxMessages = 10)
    {
        using var scope = _serviceProvider.CreateScope();
        var outboxRepository = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var rabbitMQPublisher = scope.ServiceProvider.GetService<IMessagePublisher>();
        var kafkaProducer = scope.ServiceProvider.GetService<IKafkaProducer>();

        var messages = await outboxRepository.GetUnprocessedMessagesAsync(maxMessages);
        foreach (var message in messages)
        {
            await ProcessMessageAsync(message, rabbitMQPublisher, kafkaProducer, outboxRepository);
        }

        await unitOfWork.SaveChangesAsync();
        return messages.Count;
    }

    public async Task<int> GetPendingCountAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var outboxRepository = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();
        return await outboxRepository.GetPendingCountAsync();
    }

    public async Task CleanupProcessedMessagesAsync(int daysToKeep = 7)
    {
        using var scope = _serviceProvider.CreateScope();
        var outboxRepository = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        
        await outboxRepository.CleanupProcessedMessagesAsync(daysToKeep);
        await unitOfWork.SaveChangesAsync();
    }
}