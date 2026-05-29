// File: services/auth-service/AuthService.Infrastructure/Messaging/RabbitMQ/RabbitMQSubscriber.cs
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace AuthService.Infrastructure.Messaging.RabbitMQ;

public class RabbitMQSubscriber : BackgroundService
{
    // 👇 FIX: Implemented global alias namespace reference to bridge the blind spot compilation gap
    private readonly global::AuthService.Infrastructure.Messaging.RabbitMQ.RabbitMQConnectionManager _connectionManager;
    private readonly RabbitMQSettings _settings;
    private readonly ILogger<RabbitMQSubscriber> _logger;
    private readonly IServiceProvider _serviceProvider;
    private IChannel? _channel;

    public RabbitMQSubscriber(
        global::AuthService.Infrastructure.Messaging.RabbitMQ.RabbitMQConnectionManager connectionManager,
        IOptions<RabbitMQSettings> settings, 
        ILogger<RabbitMQSubscriber> logger,
        IServiceProvider serviceProvider)
    {
        _connectionManager = connectionManager;
        _settings = settings.Value;
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    private async Task InitializeQueueAsync()
    {
        var connection = await _connectionManager.GetConnectionAsync();
        _channel = await connection.CreateChannelAsync();

        // 🛡️ Free Tier Safety Feature: Create a Dead Letter Exchange configuration
        var deadLetterExchange = $"{_settings.AuthEventsQueue}.dlx";
        var deadLetterQueue = $"{_settings.AuthEventsQueue}.dead";
        
        await _channel.ExchangeDeclareAsync(deadLetterExchange, ExchangeType.Fanout, durable: true);
        await _channel.QueueDeclareAsync(deadLetterQueue, durable: true, exclusive: false, autoDelete: false);
        await _channel.QueueBindAsync(deadLetterQueue, deadLetterExchange, "#");

        // Bind arguments to route bad messages to our safety net DLX automatically
        var queueArgs = new Dictionary<string, object?>
        {
            { "x-dead-letter-exchange", deadLetterExchange }
        };

        // Declare the primary consumer queue with our security safety checks attached
        await _channel.QueueDeclareAsync(_settings.AuthEventsQueue, durable: true, exclusive: false, autoDelete: false, arguments: queueArgs);
        
        // Bind to primary infrastructure exchanges
        await _channel.QueueBindAsync(_settings.AuthEventsQueue, _settings.UserEventsExchange, "user.*");
        await _channel.QueueBindAsync(_settings.AuthEventsQueue, _settings.AdminEventsExchange, "admin.*");
        await _channel.QueueBindAsync(_settings.AuthEventsQueue, _settings.SecurityEventsExchange, "security.*");
        
        _logger.LogInformation("RabbitMQ subscriber queue secured, declared, and bound: {Queue}", _settings.AuthEventsQueue);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await InitializeQueueAsync();

        // Set Prefetch count to 1 to divide work evenly across your microservices
        await _channel!.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        
        consumer.ReceivedAsync += async (sender, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            var routingKey = ea.RoutingKey;

            try
            {
                await ProcessMessageAsync(routingKey, message);
                await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "🚨 Critical error handling routing key {RoutingKey}. Rejecting to DLX to save free tier limits.", routingKey);
                await _channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false);
            }
        };

        await _channel.BasicConsumeAsync(_settings.AuthEventsQueue, autoAck: false, consumer: consumer, cancellationToken: stoppingToken);
        _logger.LogInformation("RabbitMQ subscriber actively running on shared connection. Waiting for events...");
        
        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task ProcessMessageAsync(string routingKey, string message)
    {
        _logger.LogInformation("Processing microservice inbound message: {RoutingKey}", routingKey);
        
        using var scope = _serviceProvider.CreateScope();
        await Task.CompletedTask;
    }
}
