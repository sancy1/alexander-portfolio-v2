// File: AuthService.Domain/Entities/SocialUser.cs
// Purpose: Social user entity for OAuth authentication (Google/GitHub)
// Layer: Domain

using System;
using AuthService.Domain.Enums;

namespace AuthService.Domain.Entities;

public class SocialUser
{
    public Guid Id { get; private set; }
    public string ProviderId { get; private set; } = string.Empty;
    public SocialProvider Provider { get; private set; }
    public string Email { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;
    public string? AvatarUrl { get; private set; }
    public bool IsProfileComplete { get; private set; }
    public AccountStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; } // 💡 Tracking parameter added for baseline data parity
    public DateTime? LastLoginAt { get; private set; }
    
    // NEW FIELDS FOR ACCOUNT DELETION
    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAt { get; private set; }
    public DateTime? PermanentDeleteAt { get; private set; }
    public string? DeleteReason { get; private set; }

    public bool IsAdminBlocked { get; private set; }
    public string? AdminBlockReason { get; private set; }
    public DateTime? AdminBlockedAt { get; private set; }

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
    private SocialUser() { }

    public SocialUser(string providerId, SocialProvider provider, string email, string displayName)
    {
        Id = Guid.NewGuid();
        ProviderId = providerId;
        Provider = provider;
        Email = email.ToLowerInvariant();
        DisplayName = displayName;
        IsProfileComplete = false;
        Status = AccountStatus.Active;
        CreatedAt = DateTime.UtcNow;
        IsDeleted = false;
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

    public void CompleteProfile(string displayName, string? avatarUrl = null)
    {
        DisplayName = displayName;
        AvatarUrl = avatarUrl;
        IsProfileComplete = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void RecordLogin()
    {
        LastLoginAt = DateTime.UtcNow;
    }

    public void UpdateProfile(string displayName, string? avatarUrl = null)
    {
        DisplayName = displayName;
        if (avatarUrl != null)
            AvatarUrl = avatarUrl;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateAvatar(string? avatarUrl)
    {
        AvatarUrl = avatarUrl;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Suspend()
    {
        Status = AccountStatus.Suspended;
    }

    public void Activate()
    {
        Status = AccountStatus.Active;
    }

    // NEW METHODS FOR ACCOUNT DELETION
    public void SoftDelete(string reason)
    {
        IsDeleted = true;
        Status = AccountStatus.Deactivated;
        DeletedAt = DateTime.UtcNow;
        PermanentDeleteAt = DateTime.UtcNow.AddDays(30);
        DeleteReason = reason;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Restore()
    {
        if (IsAdminBlocked)
        {
            throw new InvalidOperationException("Account is blocked by admin. Cannot self-restore.");
        }
        
        IsDeleted = false;
        Status = AccountStatus.Active;
        DeletedAt = null;
        PermanentDeleteAt = null;
        DeleteReason = null;
        UpdatedAt = DateTime.UtcNow;
    }

    public void HardDelete()
    {
        // Handled via persistence tier repositories
    }

    public bool CanPermanentlyDelete()
    {
        return PermanentDeleteAt.HasValue && PermanentDeleteAt.Value <= DateTime.UtcNow;
    }

    public void AdminBlock(string reason)
    {
        IsAdminBlocked = true;
        AdminBlockReason = reason;
        AdminBlockedAt = DateTime.UtcNow;
        Status = AccountStatus.Suspended;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AdminUnblock()
    {
        IsAdminBlocked = false;
        AdminBlockReason = null;
        AdminBlockedAt = null;
        Status = AccountStatus.Active;
        UpdatedAt = DateTime.UtcNow;
    }

    public bool CanSelfRestore()
    {
        return IsDeleted && 
            !IsAdminBlocked && 
            PermanentDeleteAt.HasValue && 
            PermanentDeleteAt.Value > DateTime.UtcNow;
    }
}
