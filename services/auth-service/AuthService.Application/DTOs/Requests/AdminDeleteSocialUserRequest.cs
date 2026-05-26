// File: AuthService.Application/DTOs/Requests/AdminDeleteSocialUserRequest.cs
// Purpose: Request DTO for admin to delete social user
// Layer: Application

namespace AuthService.Application.DTOs.Requests;

public class AdminDeleteSocialUserRequest
{
    public string Reason { get; set; } = string.Empty;
    public bool PermanentDelete { get; set; } = false;
}