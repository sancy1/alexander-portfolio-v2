// File: AuthService.Infrastructure/Messaging/RabbitMQ/RabbitMQSettings.cs
// Purpose: Configuration settings for RabbitMQ connection
// Layer: Infrastructure

namespace AuthService.Infrastructure.Messaging.RabbitMQ;

public class RabbitMQSettings
{
    public string HostName { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string UserName { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string VirtualHost { get; set; } = "/";
    public string ExchangeName { get; set; } = "auth.events";
    public string QueueName { get; set; } = "auth_service_queue";
}