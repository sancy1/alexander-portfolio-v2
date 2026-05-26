namespace AuthService.Application.DTOs.Events;

public class UserModifiedEvent
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool IsBlocked { get; set; }
    public string? BlockReason { get; set; }
    public DateTime ModifiedAt { get; set; }
    public string ModifiedBy { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty; // "activated", "deactivated", "updated", "blocked", "unblocked"
}