// File: services/auth-service/AuthService.Infrastructure/Messaging/RabbitMQ/RabbitMQPublisher.cs
using System;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using AuthService.Application.Interfaces.Messaging;

namespace AuthService.Infrastructure.Messaging.RabbitMQ;

public class RabbitMQPublisher : IMessagePublisher, IAsyncDisposable
{
    // 👇 FIX: Using full global alias namespace path to prevent project-link compilation blind spots
    private readonly global::AuthService.Infrastructure.Messaging.RabbitMQ.RabbitMQConnectionManager _connectionManager;
    private readonly RabbitMQSettings _settings;
    private readonly ILogger<RabbitMQPublisher> _logger;
    private IChannel? _channel;
    private bool _exchangesInitialized;
    private bool _disposed;

    public RabbitMQPublisher(
        global::AuthService.Infrastructure.Messaging.RabbitMQ.RabbitMQConnectionManager connectionManager, 
        IOptions<RabbitMQSettings> settings, 
        ILogger<RabbitMQPublisher> logger)
    {
        _connectionManager = connectionManager;
        _settings = settings.Value;
        _logger = logger;
    }

    private async Task EnsureChannelInitializedAsync()
    {
        if (_channel is { IsClosed: false } && _exchangesInitialized) return;

        var connection = await _connectionManager.GetConnectionAsync();
        _channel = await connection.CreateChannelAsync();

        if (!_exchangesInitialized)
        {
            await _channel.ExchangeDeclareAsync(_settings.UserEventsExchange, ExchangeType.Topic, durable: true);
            await _channel.ExchangeDeclareAsync(_settings.AdminEventsExchange, ExchangeType.Topic, durable: true);
            await _channel.ExchangeDeclareAsync(_settings.SecurityEventsExchange, ExchangeType.Topic, durable: true);
            
            _exchangesInitialized = true;
        }
    }

    public async Task PublishAsync<T>(string routingKey, T message)
    {
        try
        {
            await EnsureChannelInitializedAsync();

            var json = message is JsonElement element ? element.GetRawText() : JsonSerializer.Serialize(message);
            var body = Encoding.UTF8.GetBytes(json);
            var exchange = DetermineExchange(routingKey);
            
            await _channel!.BasicPublishAsync(
                exchange: exchange, 
                routingKey: routingKey, 
                mandatory: true,
                basicProperties: new BasicProperties { DeliveryMode = DeliveryModes.Persistent },
                body: body);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish message to routing key {RoutingKey}", routingKey);
            throw;
        }
    }

    public async Task PublishEventAsync<T>(T eventMessage) where T : IEvent
    {
        await PublishAsync(eventMessage.EventType, eventMessage);
    }

    private string DetermineExchange(string routingKey)
    {
        if (routingKey.StartsWith("user.") || routingKey.StartsWith("social.user.")) return _settings.UserEventsExchange;
        if (routingKey.StartsWith("admin.")) return _settings.AdminEventsExchange;
        if (routingKey.StartsWith("security.")) return _settings.SecurityEventsExchange;
        return _settings.UserEventsExchange;
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        if (_channel != null) await _channel.CloseAsync();
        _disposed = true;
    }
}
