// File: AuthService.Infrastructure/Services/OutboxProcessorService.cs
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using AuthService.Application.Interfaces.Persistence;
using AuthService.Application.Interfaces.Messaging;
using AuthService.Infrastructure.Messaging.Kafka;
using AuthService.Domain.Entities;
using System.Text.Json;

namespace AuthService.Infrastructure.Services;

public class OutboxProcessorService : BackgroundService, IOutboxProcessorService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OutboxProcessorService> _logger;
    private readonly int _batchSize = 10;        // reduced from 20
    private readonly int _sleepSeconds = 30;     // increased from 5 — protects connection pool
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

        // Initial delay — let the app fully start before first poll
        await Task.Delay(TimeSpan.FromSeconds(15), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessBatchAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in outbox processor loop");
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

        if (!pendingMessages.Any())
            return;

        _logger.LogInformation("Processing {Count} pending outbox messages", pendingMessages.Count);

        foreach (var message in pendingMessages)
        {
            await ProcessMessageAsync(message, rabbitMQPublisher, kafkaProducer, outboxRepository);
        }

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
            var brokerTarget = message.Broker?.ToLower() ?? "rabbitmq";

            var targetRabbitMQ = brokerTarget is "all" or "both" or "rabbitmq";
            var targetKafka = brokerTarget is "all" or "both" or "kafka";

            if (targetRabbitMQ && rabbitMQPublisher != null)
                await rabbitMQPublisher.PublishAsync(message.RoutingKey, parsedPayload);

            if (targetKafka && kafkaProducer != null)
                await kafkaProducer.ProduceAsync(message.RoutingKey, message.Payload);

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
                    "🚨 Outbox message {Id} dead-lettered after {Count} attempts",
                    message.Id, _maxRetryCount);
                message.ProcessedAt = DateTime.UtcNow;
            }
            else
            {
                _logger.LogWarning(ex,
                    "Outbox retry {Count}/{Max} for message {Id}",
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
            await ProcessMessageAsync(message, rabbitMQPublisher, kafkaProducer, outboxRepository);

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