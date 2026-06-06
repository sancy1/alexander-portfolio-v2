// services/auth-service/AuthService.Application/Common/EventEnvelope.cs

// File: services/auth-service/AuthService.Application/Common/EventEnvelope.cs

using System;
using System.Text.Json.Serialization;

namespace AuthService.Application.Common;

/// <summary>
/// Standardized event envelope wrapping all outbox messages.
/// Ensures a strictly typed, consistent contract across distributed microservices.
/// </summary>
/// <typeparam name="TPayload">The strongly-typed business data payload definition.</typeparam>
public class EventEnvelope<TPayload> where TPayload : class
{
    /// <summary>
    /// Unique identifier for this specific event instance.
    /// </summary>
    [JsonPropertyName("eventId")]
    public string EventId { get; init; } = Guid.NewGuid().ToString();
    
    /// <summary>
    /// Type of event (e.g., "admin.loggedin", "social.user.loggedin").
    /// </summary>
    [JsonPropertyName("eventType")]
    public string EventType { get; init; } = string.Empty;
    
    /// <summary>
    /// Name of the upstream microservice that published this event.
    /// </summary>
    [JsonPropertyName("sourceService")]
    public string SourceService { get; init; } = "auth-service";
    
    /// <summary>
    /// Universal Timestamp (UTC) when the event originally occurred.
    /// </summary>
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
    
    /// <summary>
    /// Semantic version of the structural schema.
    /// </summary>
    [JsonPropertyName("version")]
    public string Version { get; init; } = "1.0";
    
    /// <summary>
    /// The unique system identity string of the executing user actor.
    /// </summary>
    [JsonPropertyName("userId")]
    public string? UserId { get; init; }
    
    /// <summary>
    /// The classification categorical type of user (e.g., admin, social-user).
    /// </summary>
    [JsonPropertyName("userType")]
    public string? UserType { get; init; }
    
    /// <summary>
    /// Contextual internal object hosting the distinct data payload fields.
    /// </summary>
    [JsonPropertyName("payload")]
    public TPayload Payload { get; init; } = null!;
    
    /// <summary>
    /// Traceability identifier to correlate a chain of actions across services.
    /// </summary>
    [JsonPropertyName("correlationId")]
    public string? CorrelationId { get; init; }
    
    /// <summary>
    /// Traceability identifier pointing to the direct cause of this execution event.
    /// </summary>
    [JsonPropertyName("causationId")]
    public string? CausationId { get; init; }
}