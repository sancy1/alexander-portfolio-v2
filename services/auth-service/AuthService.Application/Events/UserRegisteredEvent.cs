// File: AuthService.Application/DTOs/Events/UserRegisteredEvent.cs
using System;
using AuthService.Application.Interfaces.Messaging;

namespace AuthService.Application.DTOs.Events;

public class UserRegisteredEvent : IEvent
{
    public string EventType { get; init; } = "social.user.registered";
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
    
    // 👇 Holds the UUID from your SocialUsers 'Id' column
    public Guid UserId { get; init; } 
    public string Email { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string Provider { get; init; } = string.Empty; // Holds "google" or "github"
    public bool IsProfileComplete { get; init; } // Matches your SocialUsers column
    public string ClientIp { get; init; } = string.Empty;
    public string UserAgent { get; init; } = string.Empty;
}
