// File: AuthService.Application/DTOs/Requests/UpdatePublicProfileRequest.cs
// Purpose: Inbound Request payload schema for public metadata updates
// Layer: Application

namespace AuthService.Application.DTOs.Requests;

public sealed class UpdatePublicProfileRequest
{
    public string? FullName { get; set; }
    public string? JobTitle { get; set; }
    public string? Headline { get; set; }
    public string? Tagline { get; set; }
    public string? Bio { get; set; }
    public string? Phone { get; set; }
    public string? Location { get; set; }
    public string? Website { get; set; }
    public SocialLinksDto? SocialLinks { get; set; }
}

public sealed class SocialLinksDto
{
    public string? Twitter { get; set; }
    public string? LinkedIn { get; set; }
    public string? GitHub { get; set; }
    public string? Instagram { get; set; }
    public string? Facebook { get; set; }
    public string? YouTube { get; set; }
}
