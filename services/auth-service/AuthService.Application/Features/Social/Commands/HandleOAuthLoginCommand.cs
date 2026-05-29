// File: services/auth-service/AuthService.Application/Features/Social/Commands/HandleOAuthLoginCommand.cs
using MediatR;
using AuthService.Application.DTOs.Responses;
using AuthService.Domain.Enums;

namespace AuthService.Application.Features.Social.Commands;

public class HandleOAuthLoginCommand : IRequest<AuthResponse>
{
    public string ProviderId { get; }
    public SocialProvider Provider { get; }
    public string Email { get; }
    public string DisplayName { get; }
    public string? AvatarUrl { get; }
    
    // 👇 Context telemetry appended to carry auditing properties down from Nginx
    public string ClientIp { get; }
    public string UserAgent { get; }

    public HandleOAuthLoginCommand(
        string providerId, 
        SocialProvider provider, 
        string email, 
        string displayName,
        string clientIp,
        string userAgent,
        string? avatarUrl = null)
    {
        ProviderId = providerId;
        Provider = provider;
        Email = email;
        DisplayName = displayName;
        AvatarUrl = avatarUrl;
        ClientIp = string.IsNullOrEmpty(clientIp) ? "Unknown" : clientIp;
        UserAgent = string.IsNullOrEmpty(userAgent) ? "Unknown" : userAgent;
    }
}
