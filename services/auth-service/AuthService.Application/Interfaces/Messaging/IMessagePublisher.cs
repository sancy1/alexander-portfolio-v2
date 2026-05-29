// File: services/auth-service/AuthService.Application/Interfaces/Messaging/IMessagePublisher.cs
using System;
using System.Threading.Tasks;

namespace AuthService.Application.Interfaces.Messaging;

/// <summary>
/// Contract requirement for all historical, state-changing business events across the architecture.
/// </summary>
public interface IEvent
{
    string EventType { get; }
    DateTime OccurredAt { get; }
}

/// <summary>
/// Unified core publishing engine contract for both targeted integration messaging and domain event broadcasting.
/// </summary>
public interface IMessagePublisher
{
    /// <summary>
    /// Generic message dispatcher using an explicit routing string pattern.
    /// </summary>
    Task PublishAsync<T>(string routingKey, T message);

    /// <summary>
    /// Enforced event-driven dispatcher that extracts routing parameters directly from the event metadata.
    /// </summary>
    Task PublishEventAsync<T>(T eventMessage) where T : IEvent;
}
