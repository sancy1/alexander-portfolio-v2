// File: AuthService.Application/DTOs/Requests/RestoreSocialAccountRequest.cs
// Purpose: Request DTO for restoring soft-deleted social account
// Layer: Application

namespace AuthService.Application.DTOs.Requests;

public class RestoreSocialAccountRequest
{
    public string Email { get; set; } = string.Empty;
}