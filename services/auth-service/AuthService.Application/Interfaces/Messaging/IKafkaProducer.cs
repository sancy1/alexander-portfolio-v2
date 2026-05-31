
// services/auth-service/AuthService.Application/Interfaces/Messaging/IKafkaProducer.cs

using System.Threading;
using System.Threading.Tasks;

namespace AuthService.Application.Interfaces.Messaging;

/// <summary>
/// Kafka producer interface for event archiving (The Archive pattern)
/// </summary>
public interface IKafkaProducer
{
    /// <summary>
    /// Produce a raw pre-serialized string message to a Kafka topic
    /// </summary>
    /// <param name="topic">Target Kafka topic</param>
    /// <param name="payload">Message payload (JSON string)</param>
    /// <param name="cancellationToken">Token to abort network operations</param>
    Task ProduceAsync(string topic, string payload, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Produce a lightweight reference message using a typed object contract
    /// </summary>
    /// <typeparam name="T">The event type wrapper</typeparam>
    /// <param name="topic">Target Kafka topic name</param>
    /// <param name="payload">Lightweight tracking event object (Max 200 bytes)</param>
    /// <param name="cancellationToken">Token to abort network operations</param>
    Task ProduceAsync<T>(string topic, T payload, CancellationToken cancellationToken = default) where T : class;
    
    /// <summary>
    /// Produce a permanent audit log entry to the dedicated security topic
    /// </summary>
    /// <typeparam name="T">The audit contract type</typeparam>
    /// <param name="auditLog">Security metadata frame</param>
    /// <param name="cancellationToken">Token to abort network operations</param>
    Task ProduceAuditLogAsync<T>(T auditLog, CancellationToken cancellationToken = default) where T : class;
}
