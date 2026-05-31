using System.Threading;
using System.Threading.Tasks;

namespace AuthService.Application.Interfaces.Messaging;

/// <summary>
/// No-Op concrete fallback representation of the Archive pattern when Kafka is unavailable.
/// </summary>
public sealed class NullKafkaProducer : IKafkaProducer
{
    public Task ProduceAsync(string topic, string payload, CancellationToken cancellationToken = default) 
        => Task.CompletedTask;

    public Task ProduceAsync<T>(string topic, T payload, CancellationToken cancellationToken = default) where T : class 
        => Task.CompletedTask;

    public Task ProduceAuditLogAsync<T>(T auditLog, CancellationToken cancellationToken = default) where T : class 
        => Task.CompletedTask;
}
