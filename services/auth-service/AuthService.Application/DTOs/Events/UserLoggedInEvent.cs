// // File: AuthService.Application/DTOs/Events/UserLoggedInEvent.cs
// using System;
// using AuthService.Application.Interfaces.Messaging;

// namespace AuthService.Application.DTOs.Events;

// public class UserLoggedInEvent : IEvent
// {
//     public string EventType { get; init; } = "user.loggedin";
//     public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
    
//     // 👇 Maps perfectly to either your Admins.Id or SocialUsers.Id columns
//     public Guid UserId { get; init; } 
//     public string Email { get; init; } = string.Empty;
    
//     // 👇 CRITICAL ADDITION: Identifies the exact origin table
//     public string UserType { get; init; } = string.Empty; // "Admin" or "SocialUser"
//     public string LoginMethod { get; init; } = string.Empty; // "password", "google", "github"
//     public string ClientIp { get; init; } = string.Empty;
//     public string UserAgent { get; init; } = string.Empty;
// }

























// File: services/auth-service/AuthService.Application/DTOs/Events/UserLoggedInEvent.cs

using System;

namespace AuthService.Application.DTOs.Events;

/// <summary>
/// Data Transfer Object representing the clean business payload for a successful user login event.
/// </summary>
public class UserLoggedInEvent
{
    /// <summary>
    /// Unique identifier linking directly to either the Admins or SocialUsers table record.
    /// </summary>
    public string UserId { get; init; } = string.Empty;
    
    public string Email { get; init; } = string.Empty;
    
    public string DisplayName { get; init; } = string.Empty;
    
    public string LoginMethod { get; init; } = string.Empty; 
    
    public string LoginTime { get; init; } = string.Empty;
    
    public string ClientIp { get; init; } = string.Empty;
    
    public string UserAgent { get; init; } = string.Empty;
}