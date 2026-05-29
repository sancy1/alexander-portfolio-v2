using System;
using System.Text.Json;
using System.Threading.Tasks;
using AuthService.Domain.Entities;
using AuthService.Application.Interfaces.Persistence;

namespace AuthService.Application.Common;

public static class OutboxHelper
{
    // ============================================================================
    // METHOD 1: NEW ATOMIC VERSION
    // ============================================================================
    public static async Task AddToOutboxAsync(
        IOutboxRepository outboxRepository,
        string eventType,
        string routingKey,
        string broker,
        object payload)
    {
        var outboxMessage = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            EventType = eventType,
            RoutingKey = routingKey,
            Broker = broker,
            Payload = JsonSerializer.Serialize(payload),
            CreatedAt = DateTime.UtcNow,
            RetryCount = 0,
            ProcessedAt = null 
            // 👇 REMOVED IsProcessed line entirely because ProcessedAt = null handles it!
        };

        await outboxRepository.AddAsync(outboxMessage);
    }

    // ============================================================================
    // METHOD 2: LEGACY COMPATIBILITY VERSION
    // ============================================================================
    public static async Task AddToOutboxAsync(
        IOutboxRepository outboxRepository,
        IUnitOfWork unitOfWork, 
        string eventType,
        string routingKey,
        string broker,
        object payload)
    {
        await AddToOutboxAsync(outboxRepository, eventType, routingKey, broker, payload);
    }
}
