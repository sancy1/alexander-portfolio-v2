using MediatR;
using AuthService.Application.DTOs.Requests;

namespace AuthService.Application.Features.Social.Commands;

public sealed record UpdateSocialPublicProfileCommand(
    Guid UserId,
    string? FullName,
    string? JobTitle,
    string? Headline,
    string? Tagline,
    string? Bio,
    string? Phone,
    string? Location,
    string? Website,
    SocialLinksDto? SocialLinks
) : IRequest<SocialProfileCommandResponse>;

public sealed record SocialProfileCommandResponse(
    bool Success, 
    string Message, 
    object? Profile = null
);
