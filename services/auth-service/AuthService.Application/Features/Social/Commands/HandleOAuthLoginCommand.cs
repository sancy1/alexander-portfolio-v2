// using MediatR;
// using AuthService.Application.DTOs.Responses;
// using AuthService.Domain.Enums;

// namespace AuthService.Application.Features.Social.Commands;

// public class HandleOAuthLoginCommand : IRequest<AuthResponse>
// {
//     public string ProviderId { get; }
//     public SocialProvider Provider { get; }
//     public string Email { get; }
//     public string DisplayName { get; }

//     public HandleOAuthLoginCommand(string providerId, SocialProvider provider, string email, string displayName)
//     {
//         ProviderId = providerId;
//         Provider = provider;
//         Email = email;
//         DisplayName = displayName;
//     }
// }


























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

    public HandleOAuthLoginCommand(
        string providerId, 
        SocialProvider provider, 
        string email, 
        string displayName,
        string? avatarUrl = null)
    {
        ProviderId = providerId;
        Provider = provider;
        Email = email;
        DisplayName = displayName;
        AvatarUrl = avatarUrl;
    }
}