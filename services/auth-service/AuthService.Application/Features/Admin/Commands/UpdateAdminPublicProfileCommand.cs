using MediatR;
using AuthService.Application.DTOs.Requests;

namespace AuthService.Application.Features.Admin.Commands;

public sealed record UpdateAdminPublicProfileCommand(
    Guid AdminId,
    string? FullName,
    string? JobTitle,
    string? Headline,
    string? Tagline,
    string? Bio,
    string? Phone,
    string? Location,
    string? Website,
    SocialLinksDto? SocialLinks
) : IRequest<PublicProfileCommandResponse>;

public sealed record PublicProfileCommandResponse(
    bool Success, 
    string Message, 
    object? Profile = null
);
