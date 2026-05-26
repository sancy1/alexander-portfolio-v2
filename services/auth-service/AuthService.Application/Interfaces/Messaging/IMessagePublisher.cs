// File: AuthService.Application/Interfaces/Messaging/IMessagePublisher.cs
// Purpose: Interface for message publishing to RabbitMQ
// Layer: Application

namespace AuthService.Application.Interfaces.Messaging;

public interface IMessagePublisher
{
    Task PublishAsync<T>(string routingKey, T message);
}