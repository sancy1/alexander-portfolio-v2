// File: AuthService.Application/Features/Social/Commands/CompleteUserProfileCommand.cs
// Purpose: Command for completing social user profile
// Layer: Application

using MediatR;
using AuthService.Application.DTOs.Responses;

namespace AuthService.Application.Features.Social.Commands;

public class CompleteUserProfileCommand : IRequest<AuthResponse>
{
    public Guid UserId { get; set; }
    public string DisplayName { get; set; }
    public string? AvatarUrl { get; set; }

    public CompleteUserProfileCommand(Guid userId, string displayName, string? avatarUrl)
    {
        UserId = userId;
        DisplayName = displayName;
        AvatarUrl = avatarUrl;
    }
}