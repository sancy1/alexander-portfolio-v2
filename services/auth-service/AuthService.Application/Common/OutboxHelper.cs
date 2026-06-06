// // File: AuthService.Application/Common/OutboxHelper.cs
// using System;
// using System.Text.Json;
// using System.Threading.Tasks;
// using AuthService.Domain.Entities;
// using AuthService.Application.Interfaces.Persistence;

// namespace AuthService.Application.Common;

// public static class OutboxHelper
// {
//     // Shared serialization settings to guarantee camelCase parity across microservices
//     private static readonly JsonSerializerOptions SerializerOptions = new()
//     {
//         PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
//         WriteIndented = false
//     };

//     // ============================================================================
//     // METHOD 1: NEW ATOMIC VERSION
//     // ============================================================================
//     public static async Task AddToOutboxAsync(
//         IOutboxRepository outboxRepository,
//         string eventType,
//         string routingKey,
//         string broker,
//         object payload)
//     {
//         var outboxMessage = new OutboxMessage
//         {
//             Id = Guid.NewGuid(),
//             EventType = eventType,
//             RoutingKey = routingKey,
//             Broker = broker.ToLowerInvariant(), // 🧠 Rules Applied: Guarantees string lowercase sorting safety
//             Payload = JsonSerializer.Serialize(payload, SerializerOptions), // Standardized format output
//             CreatedAt = DateTime.UtcNow,
//             RetryCount = 0,
//             ProcessedAt = null
//         };

//         await outboxRepository.AddAsync(outboxMessage);
//     }

//     // ============================================================================
//     // METHOD 2: LEGACY COMPATIBILITY VERSION
//     // ============================================================================
//     public static async Task AddToOutboxAsync(
//         IOutboxRepository outboxRepository,
//         IUnitOfWork unitOfWork, 
//         string eventType,
//         string routingKey,
//         string broker,
//         object payload)
//     {
//         // Forwards arguments to Method 1 to maintain backwards-compatible runtime execution lines
//         await AddToOutboxAsync(outboxRepository, eventType, routingKey, broker, payload);
//     }
// }

























// File: services/auth-service/AuthService.Application/Common/OutboxHelper.cs

using System;
using System.Text.Json;
using System.Threading.Tasks;
using AuthService.Domain.Entities;
using AuthService.Application.Interfaces.Persistence;

namespace AuthService.Application.Common;

/// <summary>
/// Centralized utility helper to manage atomic database persistence of outbound events 
/// via the transactional Outbox Pattern.
/// </summary>
public static class OutboxHelper
{
    // Shared serialization settings to guarantee camelCase parity across microservices
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    // ============================================================================
    // METHOD 1: ORIGINAL VERSION (Preserved for seamless fallback integration)
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
            Broker = broker?.ToLowerInvariant() ?? "kafka", // Defensive fallback protection
            Payload = JsonSerializer.Serialize(payload, SerializerOptions),
            CreatedAt = DateTime.UtcNow,
            RetryCount = 0,
            ProcessedAt = null
        };

        await outboxRepository.AddAsync(outboxMessage);
    }

    // ============================================================================
    // METHOD 2: LEGACY COMPATIBILITY VERSION (Preserved unchanged)
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

    // ============================================================================
    // METHOD 3: NEW STRONGLY-TYPED STANDARDIZED ENVELOPE VERSION
    // ============================================================================
    /// <summary>
    /// Adds an event to the outbox using the generic, standardized EventEnvelope format.
    /// Recommended for all new features to enforce strict cross-service contract compliance.
    /// </summary>
    /// <typeparam name="TPayload">The reference type of the business data payload.</typeparam>
    public static async Task AddToOutboxAsync<TPayload>(
        IOutboxRepository outboxRepository,
        string eventType,
        string routingKey,
        string broker,
        TPayload payload,
        string? userId = null,
        string? userType = null,
        string? correlationId = null) where TPayload : class
    {
        // 1. Leverage modern generic initialization and default property values
        var envelope = new EventEnvelope<TPayload>
        {
            EventType = eventType,
            UserId = userId,
            UserType = userType,
            Payload = payload,
            CorrelationId = correlationId
        };

        // 2. Enforce absolute correlation identity parity across the database row and JSON wire format
        var outboxMessage = new OutboxMessage
        {
            Id = Guid.Parse(envelope.EventId), // 🧠 Synchronization Rule: Keeps DB Primary Key matching the internal JSON eventId
            EventType = eventType,
            RoutingKey = routingKey,
            Broker = broker?.ToLowerInvariant() ?? "kafka",
            Payload = JsonSerializer.Serialize(envelope, SerializerOptions),
            CreatedAt = DateTime.UtcNow,
            RetryCount = 0,
            ProcessedAt = null
        };

        await outboxRepository.AddAsync(outboxMessage);
    }

    // ============================================================================
    // METHOD 4: STANDARDIZED ENVELOPE WITH UNITOFWORK OVERLOAD
    // ============================================================================
    /// <summary>
    /// Adds an event to the outbox using the standardized generic EventEnvelope format within an explicit Unit of Work context.
    /// </summary>
    /// <typeparam name="TPayload">The reference type of the business data payload.</typeparam>
    public static async Task AddToOutboxAsync<TPayload>(
        IOutboxRepository outboxRepository,
        IUnitOfWork unitOfWork,
        string eventType,
        string routingKey,
        string broker,
        TPayload payload,
        string? userId = null,
        string? userType = null,
        string? correlationId = null) where TPayload : class
    {
        // Directly delegates to Method 3 to maintain a clean DRY architecture
        await AddToOutboxAsync(outboxRepository, eventType, routingKey, broker, payload, userId, userType, correlationId);
    }
}