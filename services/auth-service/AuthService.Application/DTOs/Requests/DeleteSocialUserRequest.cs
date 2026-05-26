// File: AuthService.Application/DTOs/Requests/DeleteSocialUserRequest.cs
// Purpose: Request DTO for social user self-deletion
// Layer: Application

namespace AuthService.Application.DTOs.Requests;

public class DeleteSocialUserRequest
{
    public string ConfirmEmail { get; set; } = string.Empty;
    public bool PermanentDelete { get; set; } = false;
    public string? Reason { get; set; }
}