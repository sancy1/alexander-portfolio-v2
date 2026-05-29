// File: services/auth-service/AuthService.Infrastructure/Services/OutboxProcessorService.cs     
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using AuthService.Application.Interfaces.Persistence;
using AuthService.Application.Interfaces.Messaging;
using AuthService.Infrastructure.Messaging.Kafka;
using AuthService.Domain.Entities;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;

namespace AuthService.Infrastructure.Services;

public class OutboxProcessorService : BackgroundService, IOutboxProcessorService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OutboxProcessorService> _logger;
    private readonly int _batchSize = 20;
    private readonly int _sleepSeconds = 5;
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
        _logger.LogInformation("🚀 Outbox Processor Service started...");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var outboxRepository = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();
                var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                var rabbitMQPublisher = scope.ServiceProvider.GetService<IMessagePublisher>();
                var kafkaProducer = scope.ServiceProvider.GetService<IKafkaProducer>();

                var pendingMessages = await outboxRepository.GetUnprocessedMessagesAsync(_batchSize);
                
                if (pendingMessages.Any())
                {
                    _logger.LogInformation("Processing {Count} pending outbox messages", pendingMessages.Count);
                    
                    foreach (var message in pendingMessages)
                    {
                        await ProcessMessageWithDualBrokerSupportAsync(message, rabbitMQPublisher, kafkaProducer, outboxRepository);
                    }
                    
                    await unitOfWork.SaveChangesAsync(stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing outbox messages loop execution");
            }

            await Task.Delay(TimeSpan.FromSeconds(_sleepSeconds), stoppingToken);
        }
    }

    private async Task ProcessMessageWithDualBrokerSupportAsync(
        OutboxMessage message, 
        IMessagePublisher? rabbitMQPublisher, 
        IKafkaProducer? kafkaProducer,
        IOutboxRepository outboxRepository)
    {
        try
        {
            var parsedPayload = JsonDocument.Parse(message.Payload).RootElement;
            bool targetRabbitMQ = false;
            bool targetKafka = false;

            string brokerTarget = message.Broker?.ToLower() ?? "rabbitmq";
            if (brokerTarget == "all" || brokerTarget == "both")
            {
                targetRabbitMQ = true;
                targetKafka = true;
            }
            else if (brokerTarget == "rabbitmq") targetRabbitMQ = true;
            else if (brokerTarget == "kafka") targetKafka = true;

            if (targetRabbitMQ && rabbitMQPublisher != null)
            {
                await rabbitMQPublisher.PublishAsync(message.RoutingKey, parsedPayload);
            }

            if (targetKafka && kafkaProducer != null)
            {
                await kafkaProducer.ProduceAsync(message.RoutingKey, message.Payload);
            }

            // 👇 FIX: Modified directly via your concrete entity columns instead of missing domain wrapper methods
            message.ProcessedAt = DateTime.UtcNow;
            message.Error = null; 
            
            await outboxRepository.UpdateAsync(message);
        }
        catch (Exception ex)
        {
            // 👇 FIX: Directly increments database columns on runtime exceptions
            message.RetryCount += 1;
            message.Error = ex.Message;
            
            if (message.RetryCount >= _maxRetryCount)
            {
                _logger.LogError(ex, "🚨 Outbox row {Id} dead-lettered after {Count} attempts.", message.Id, _maxRetryCount);
                // We set ProcessedAt to current time so the poller stops fetching this broken record
                message.ProcessedAt = DateTime.UtcNow; 
            }
            else
            {
                _logger.LogWarning(ex, "Outbox delivery retry {Count}/{Max} for message {Id}", message.RetryCount, _maxRetryCount, message.Id);
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
        var processedCount = 0;

        foreach (var message in messages)
        {
            await ProcessMessageWithDualBrokerSupportAsync(message, rabbitMQPublisher, kafkaProducer, outboxRepository);
            processedCount++;
        }

        await unitOfWork.SaveChangesAsync();
        return processedCount;
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
