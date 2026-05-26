using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace AuthService.Infrastructure.Messaging.RabbitMQ;

public class RabbitMQSubscriber : BackgroundService
{
    private readonly IConnection _connection;
    private readonly IChannel _channel;
    private readonly RabbitMQSettings _settings;

    public RabbitMQSubscriber(IOptions<RabbitMQSettings> settings)
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

        // Declare queue and bind to exchanges
        _channel.QueueDeclareAsync(_settings.QueueName, durable: true, exclusive: false, autoDelete: false).GetAwaiter().GetResult();
        _channel.QueueBindAsync(_settings.QueueName, "user.events", "user.*").GetAwaiter().GetResult();
        _channel.QueueBindAsync(_settings.QueueName, "admin.events", "admin.*").GetAwaiter().GetResult();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var consumer = new AsyncEventingBasicConsumer(_channel);
        
        consumer.ReceivedAsync += async (sender, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            var routingKey = ea.RoutingKey;

            await ProcessMessage(routingKey, message);

            await _channel.BasicAckAsync(ea.DeliveryTag, false);
        };

        await _channel.BasicConsumeAsync(_settings.QueueName, autoAck: false, consumer: consumer, cancellationToken: stoppingToken);
        
        // Keep the service running
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private Task ProcessMessage(string routingKey, string message)
    {
        Console.WriteLine($"Received message: {routingKey} - {message}");
        return Task.CompletedTask;
    }
}