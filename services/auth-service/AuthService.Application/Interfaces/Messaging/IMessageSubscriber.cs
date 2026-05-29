// File: services/auth-service/AuthService.Application/Interfaces/Messaging/IMessageSubscriber.cs   
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AuthService.Application.Interfaces.Messaging;

public interface IMessageSubscriber
{
    /// <summary>
    /// Register a callback handler for a specific message type and routing key.
    /// </summary>
    /// <typeparam name="T">The type of the event DTO (e.g., UserRegisteredEvent)</typeparam>
    /// <param name="routingKey">The RabbitMQ topic pattern to bind to (e.g., "user.registered")</param>
    /// <param name="handler">The asynchronous execution delegate task</param>
    void Subscribe<T>(string routingKey, Func<T, Task> handler) where T : class;
}
