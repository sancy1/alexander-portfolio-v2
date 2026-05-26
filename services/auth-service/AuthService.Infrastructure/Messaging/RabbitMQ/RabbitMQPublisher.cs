using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using AuthService.Application.Interfaces.Messaging;

namespace AuthService.Infrastructure.Messaging.RabbitMQ;

public class RabbitMQPublisher : IMessagePublisher, IAsyncDisposable
{
    private readonly IConnection _connection;
    private readonly IChannel _channel;
    private readonly RabbitMQSettings _settings;

    public RabbitMQPublisher(IOptions<RabbitMQSettings> settings)
    {
        _settings = settings.Value;

        var factory = new ConnectionFactory
        {
            HostName = _settings.HostName,
            Port = _settings.Port,
            UserName = _settings.UserName,
            Password = _settings.Password,
            VirtualHost = _settings.VirtualHost
        };

        _connection = factory.CreateConnectionAsync().GetAwaiter().GetResult();
        _channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();

        // Declare exchanges
        _channel.ExchangeDeclareAsync("user.events", ExchangeType.Topic, durable: true).GetAwaiter().GetResult();
        _channel.ExchangeDeclareAsync("admin.events", ExchangeType.Topic, durable: true).GetAwaiter().GetResult();
        _channel.ExchangeDeclareAsync("security.events", ExchangeType.Topic, durable: true).GetAwaiter().GetResult();
    }

    public async Task PublishAsync<T>(string routingKey, T message)
    {
        var json = JsonSerializer.Serialize(message);
        var body = Encoding.UTF8.GetBytes(json);

        var exchange = routingKey.Split('.')[0] switch
        {
            "user" => "user.events",
            "admin" => "admin.events",
            "security" => "security.events",
            _ => "auth.events"
        };

        await _channel.BasicPublishAsync(exchange, routingKey, body: body);
    }

    public async ValueTask DisposeAsync()
    {
        await _channel.CloseAsync();
        await _connection.CloseAsync();
    }
}