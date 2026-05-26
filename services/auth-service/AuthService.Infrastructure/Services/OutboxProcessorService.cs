// File: AuthService.Infrastructure/Services/OutboxProcessorService.cs
// Purpose: Processes outbox messages manually (no background worker for free tier safety)
// Layer: Infrastructure

using Microsoft.Extensions.Logging;
using AuthService.Application.Interfaces.Persistence;
using AuthService.Application.Interfaces.Messaging;
using AuthService.Application.Interfaces.Services;  // ADD THIS
using AuthService.Infrastructure.Messaging.Kafka;
using AuthService.Domain.Entities;

namespace AuthService.Infrastructure.Services;

public class OutboxProcessorService : IOutboxProcessorService
{
    private readonly IOutboxRepository _outboxRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMessagePublisher _rabbitMQPublisher;
    private readonly IKafkaProducer _kafkaProducer;
    private readonly ILogger<OutboxProcessorService> _logger;

    public OutboxProcessorService(
        IOutboxRepository outboxRepository,
        IUnitOfWork unitOfWork,
        IMessagePublisher rabbitMQPublisher,
        IKafkaProducer kafkaProducer,
        ILogger<OutboxProcessorService> logger)
    {
        _outboxRepository = outboxRepository;
        _unitOfWork = unitOfWork;
        _rabbitMQPublisher = rabbitMQPublisher;
        _kafkaProducer = kafkaProducer;
        _logger = logger;
    }

    public async Task<int> ProcessPendingMessagesAsync(int maxMessages = 10)
    {
        var messages = await _outboxRepository.GetUnprocessedMessagesAsync(maxMessages);
        
        if (!messages.Any())
        {
            return 0;
        }

        _logger.LogInformation("Processing {Count} pending outbox messages", messages.Count);
        var processedCount = 0;

        foreach (var message in messages)
        {
            try
            {
                var success = await SendToBrokerAsync(message);
                
                if (success)
                {
                    message.ProcessedAt = DateTime.UtcNow;
                    await _outboxRepository.UpdateAsync(message);
                    processedCount++;
                    _logger.LogInformation("Successfully processed message {Id} for broker {Broker}", message.Id, message.Broker);
                }
            }
            catch (Exception ex)
            {
                message.RetryCount++;
                message.Error = ex.Message;
                await _outboxRepository.UpdateAsync(message);
                _logger.LogWarning(ex, "Failed to process message {Id}, retry {RetryCount}/3", message.Id, message.RetryCount);
            }
        }

        await _unitOfWork.SaveChangesAsync();
        return processedCount;
    }

    private async Task<bool> SendToBrokerAsync(OutboxMessage message)
    {
        switch (message.Broker.ToLower())
        {
            case "rabbitmq":
                if (_rabbitMQPublisher != null)
                {
                    await _rabbitMQPublisher.PublishAsync(message.RoutingKey, message.Payload);
                    return true;
                }
                _logger.LogWarning("RabbitMQ publisher not available");
                return false;

            case "kafka":
                if (_kafkaProducer != null)
                {
                    await _kafkaProducer.ProduceAsync(message.RoutingKey, message.Payload);
                    return true;
                }
                _logger.LogWarning("Kafka producer not available");
                return false;

            default:
                _logger.LogWarning("Unknown broker: {Broker}", message.Broker);
                return true;
        }
    }

    public async Task<int> GetPendingCountAsync()
    {
        return await _outboxRepository.GetPendingCountAsync();
    }

    public async Task CleanupOldMessagesAsync(int daysToKeep = 7)
    {
        await _outboxRepository.CleanupProcessedMessagesAsync(daysToKeep);
        _logger.LogInformation("Cleaned up outbox messages older than {Days} days", daysToKeep);
    }
}