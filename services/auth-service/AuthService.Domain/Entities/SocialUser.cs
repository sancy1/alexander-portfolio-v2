// File: AuthService.Domain/Entities/SocialUser.cs
// Purpose: Social user entity for OAuth authentication (Google/GitHub)
// Layer: Domain

using AuthService.Domain.Enums;

namespace AuthService.Domain.Entities;

public class SocialUser
{
    public Guid Id { get; private set; }
    public string ProviderId { get; private set; }
    public SocialProvider Provider { get; private set; }
    public string Email { get; private set; }
    public string DisplayName { get; private set; }
    public string? AvatarUrl { get; private set; }
    public bool IsProfileComplete { get; private set; }
    public AccountStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? LastLoginAt { get; private set; }
    
    // NEW FIELDS FOR ACCOUNT DELETION
    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAt { get; private set; }
    public DateTime? PermanentDeleteAt { get; private set; }
    public string? DeleteReason { get; private set; }

    public bool IsAdminBlocked { get; private set; }
    public string? AdminBlockReason { get; private set; }
    public DateTime? AdminBlockedAt { get; private set; }

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

    public void CompleteProfile(string displayName, string? avatarUrl = null)
    {
        DisplayName = displayName;
        AvatarUrl = avatarUrl;
        IsProfileComplete = true;
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
    }

    public void UpdateAvatar(string? avatarUrl)
    {
        AvatarUrl = avatarUrl;
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
    }

    public void Restore()
    {
        // Only restore if not admin-blocked
        if (IsAdminBlocked)
        {
            throw new InvalidOperationException("Account is blocked by admin. Cannot self-restore.");
        }
        
        IsDeleted = false;
        Status = AccountStatus.Active;
        DeletedAt = null;
        PermanentDeleteAt = null;
        DeleteReason = null;
    }

    public void HardDelete()
    {
        // This is just a marker - actual deletion happens in repository
        // The record will be removed from database
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
    }

    public void AdminUnblock()
    {
        IsAdminBlocked = false;
        AdminBlockReason = null;
        AdminBlockedAt = null;
        Status = AccountStatus.Active;
    }

    public bool CanSelfRestore()
    {
        // User can only self-restore if:
        // 1. Account is soft-deleted
        // 2. Within 30 days
        // 3. NOT admin-blocked
        return IsDeleted && 
            !IsAdminBlocked && 
            PermanentDeleteAt.HasValue && 
            PermanentDeleteAt.Value > DateTime.UtcNow;
    }

}