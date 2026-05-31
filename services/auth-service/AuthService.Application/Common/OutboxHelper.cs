// File: AuthService.Application/Common/OutboxHelper.cs
using System;
using System.Text.Json;
using System.Threading.Tasks;
using AuthService.Domain.Entities;
using AuthService.Application.Interfaces.Persistence;

namespace AuthService.Application.Common;

public static class OutboxHelper
{
    // Shared serialization settings to guarantee camelCase parity across microservices
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

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
            Broker = broker.ToLowerInvariant(), // 🧠 Rules Applied: Guarantees string lowercase sorting safety
            Payload = JsonSerializer.Serialize(payload, SerializerOptions), // Standardized format output
            CreatedAt = DateTime.UtcNow,
            RetryCount = 0,
            ProcessedAt = null
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
        // Forwards arguments to Method 1 to maintain backwards-compatible runtime execution lines
        await AddToOutboxAsync(outboxRepository, eventType, routingKey, broker, payload);
    }
}
