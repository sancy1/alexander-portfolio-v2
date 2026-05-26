// File: AuthService.Infrastructure/Messaging/Kafka/KafkaConsumer.cs
// Purpose: Consumes messages from Kafka topics for audit log processing
// Layer: Infrastructure

using System.Text;
using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace AuthService.Infrastructure.Messaging.Kafka;

public interface IKafkaConsumer
{
    Task StartConsumingAsync(CancellationToken cancellationToken);
}

public class KafkaConsumer : BackgroundService, IKafkaConsumer
{
    private readonly IConsumer<string, string> _consumer;
    private readonly KafkaSettings _settings;

    public KafkaConsumer(IOptions<KafkaSettings> settings)
    {
        _settings = settings.Value;

        var config = new ConsumerConfig
        {
            BootstrapServers = _settings.BootstrapServers,
            GroupId = _settings.ConsumerGroupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };

        _consumer = new ConsumerBuilder<string, string>(config).Build();
        _consumer.Subscribe($"{_settings.TopicPrefix}-audit-logs");
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return Task.Run(() => StartConsumingAsync(stoppingToken), stoppingToken);
    }

    public async Task StartConsumingAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var consumeResult = _consumer.Consume(cancellationToken);

                if (consumeResult != null)
                {
                    await ProcessMessage(consumeResult.Message.Value);
                    _consumer.Commit(consumeResult);
                }
            }
            catch (ConsumeException ex)
            {
                Console.WriteLine($"Kafka consume error: {ex.Error.Reason}");
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        _consumer.Close();
    }

    private Task ProcessMessage(string message)
    {
        // Process audit log message
        // This can be extended to store in database or forward to analytics
        Console.WriteLine($"Audit log received: {message}");
        return Task.CompletedTask;
    }
}