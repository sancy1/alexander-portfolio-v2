using System;

namespace AuthService.Application.DTOs.Responses;

public class AdminProfileResponse
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? AvatarUrl { get; set; } 

    // ============================================================================
    // NEW PUBLIC PROFILE RETRIEVAL DTO PROPERTIES
    // ============================================================================
    public string? FullName { get; set; }
    public string? JobTitle { get; set; }
    public string? Headline { get; set; }
    public string? Tagline { get; set; }
    public string? Bio { get; set; }
    public string? Phone { get; set; }
    public string? Location { get; set; }
    public string? Website { get; set; }
    public string? SocialLinks { get; set; } // Raw string representation safely unwrapped at API Controller layer
}
