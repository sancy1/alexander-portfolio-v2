// File: AuthService.Domain/Entities/Admin.cs
// Purpose: Admin entity for credential-based authentication
// Layer: Domain

using System;
using AuthService.Domain.Enums;

namespace AuthService.Domain.Entities;

public class Admin
{
    public Guid Id { get; private set; }
    public string Username { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public string? AvatarUrl { get; private set; }
    public UserRole Role { get; private set; }
    public AccountStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    public DateTime? LastLoginAt { get; private set; }
    public string? PasswordResetToken { get; private set; }
    public DateTime? PasswordResetTokenExpiry { get; private set; }

    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAt { get; private set; }
    public DateTime? PermanentDeleteAt { get; private set; }
    public string? DeleteReason { get; private set; }

    // ============================================================================
    // NEW PROFILE FIELDS (Nullable for smooth database parity)
    // ============================================================================
    public string? FullName { get; private set; }
    public string? JobTitle { get; private set; }
    public string? Headline { get; private set; }
    public string? Tagline { get; private set; }
    public string? Bio { get; private set; }
    public string? Phone { get; private set; }
    public string? Location { get; private set; }
    public string? Website { get; private set; }
    public string? SocialLinks { get; private set; } // Stored as JSON string representation

    // EF Core constructor
    private Admin() { }

    public Admin(string username, string email, string passwordHash, UserRole role = UserRole.Admin)
    {
        Id = Guid.NewGuid();
        Username = username;
        Email = email.ToLowerInvariant();
        PasswordHash = passwordHash;
        Role = role;
        Status = AccountStatus.Active;
        CreatedAt = DateTime.UtcNow;
    }

    // ============================================================================
    // PUBLIC PROFILE BEHAVIOR METHOD
    // ============================================================================
    public void UpdatePublicProfile(
        string? fullName = null,
        string? jobTitle = null,
        string? headline = null,
        string? tagline = null,
        string? bio = null,
        string? phone = null,
        string? location = null,
        string? website = null,
        string? socialLinks = null)
    {
        if (fullName != null) FullName = fullName;
        if (jobTitle != null) JobTitle = jobTitle;
        if (headline != null) Headline = headline;
        if (tagline != null) Tagline = tagline;
        if (bio != null) Bio = bio;
        if (phone != null) Phone = phone;
        if (location != null) Location = location;
        if (website != null) Website = website;
        if (socialLinks != null) SocialLinks = socialLinks;
        
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateProfile(string? username = null, string? email = null)
    {
        if (!string.IsNullOrWhiteSpace(username))
            Username = username;
        if (!string.IsNullOrWhiteSpace(email))
            Email = email.ToLowerInvariant();
        UpdatedAt = DateTime.UtcNow;
    }

    public void ChangePassword(string newPasswordHash)
    {
        PasswordHash = newPasswordHash;
        UpdatedAt = DateTime.UtcNow;
    }

    public void RecordLogin()
    {
        LastLoginAt = DateTime.UtcNow;
    }

    public void GeneratePasswordResetToken()
    {
        PasswordResetToken = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
        PasswordResetTokenExpiry = DateTime.UtcNow.AddHours(1);
    }

    public bool VerifyResetToken(string token)
    {
        return PasswordResetToken == token && PasswordResetTokenExpiry > DateTime.UtcNow;
    }

    public void ClearPasswordResetToken()
    {
        PasswordResetToken = null;
        PasswordResetTokenExpiry = null;
    }

    public void Suspend()
    {
        Status = AccountStatus.Suspended;
    }

    public void Activate()
    {
        Status = AccountStatus.Active;
    }

    public void Deactivate()
    {
        Status = AccountStatus.Deactivated;
    }

    public void UpdateAvatar(string? avatarUrl)
    {
        AvatarUrl = avatarUrl;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SoftDelete(string reason)
    {
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        PermanentDeleteAt = DateTime.UtcNow.AddDays(30);
        DeleteReason = reason;
        Status = AccountStatus.Deactivated;
    }

    public void Restore()
    {
        IsDeleted = false;
        DeletedAt = null;
        PermanentDeleteAt = null;
        DeleteReason = null;
        Status = AccountStatus.Active;
    }

    public bool CanPermanentlyDelete()
    {
        return PermanentDeleteAt.HasValue && PermanentDeleteAt.Value <= DateTime.UtcNow;
    }
}
