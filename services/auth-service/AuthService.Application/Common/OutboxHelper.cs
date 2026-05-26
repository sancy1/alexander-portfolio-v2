// File: AuthService.Application/Common/OutboxHelper.cs
// Purpose: Helper for adding messages to outbox
// Layer: Application

using System.Text.Json;
using AuthService.Domain.Entities;
using AuthService.Application.Interfaces.Persistence;

namespace AuthService.Application.Common;

public static class OutboxHelper
{
    public static async Task AddToOutboxAsync(
        IOutboxRepository outboxRepository,
        IUnitOfWork unitOfWork,
        string eventType,
        string routingKey,
        string broker,
        object payload)
    {
        var outboxMessage = new OutboxMessage
        {
            EventType = eventType,
            RoutingKey = routingKey,
            Broker = broker,
            Payload = JsonSerializer.Serialize(payload)
        };

        await outboxRepository.AddAsync(outboxMessage);
        await unitOfWork.SaveChangesAsync();
    }
}