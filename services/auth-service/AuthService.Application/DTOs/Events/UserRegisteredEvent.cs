// // File: AuthService.Application/DTOs/Events/UserRegisteredEvent.cs
// using System;
// using AuthService.Application.Interfaces.Messaging;

// namespace AuthService.Application.DTOs.Events;

// public class UserRegisteredEvent : IEvent
// {
//     public string EventType { get; init; } = "social.user.registered";
//     public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
    
//     // 👇 Holds the UUID from your SocialUsers 'Id' column
//     public Guid UserId { get; init; } 
//     public string Email { get; init; } = string.Empty;
//     public string DisplayName { get; init; } = string.Empty;
//     public string Provider { get; init; } = string.Empty; // Holds "google" or "github"
//     public bool IsProfileComplete { get; init; } // Matches your SocialUsers column
//     public string ClientIp { get; init; } = string.Empty;
//     public string UserAgent { get; init; } = string.Empty;
// }


























// File: services/auth-service/AuthService.Application/DTOs/Events/UserRegisteredEvent.cs

using System;

namespace AuthService.Application.DTOs.Events;

/// <summary>
/// Data Transfer Object representing the clean business payload for a social user registration event.
/// </summary>
public class UserRegisteredEvent
{
    /// <summary>
    /// Holds the unique identity UUID matching the SocialUsers database table records.
    /// </summary>
    public Guid UserId { get; init; } 
    
    public string Email { get; init; } = string.Empty;
    
    public string DisplayName { get; init; } = string.Empty;
    
    /// <summary>
    /// Holds the exact social identity client host (e.g., "google", "github").
    /// </summary>
    public string Provider { get; init; } = string.Empty; 
    
    /// <summary>
    /// Verification flag mapping the profile status requirements.
    /// </summary>
    public bool IsProfileComplete { get; init; }
    
    /// <summary>
    /// Enforced clean cross-language ISO-8601 formatting string for consumer service engines.
    /// </summary>
    public string RegisteredAt { get; init; } = string.Empty;
    
    public string ClientIp { get; init; } = string.Empty;
    
    public string UserAgent { get; init; } = string.Empty;
}