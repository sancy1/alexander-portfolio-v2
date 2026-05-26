// File: AuthService.Application/DTOs/Requests/CompleteProfileRequest.cs
// Purpose: Request DTO for completing social user profile
// Layer: Application

namespace AuthService.Application.DTOs.Requests;

public class CompleteProfileRequest
{
    public string DisplayName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }  // Optional - can use default social avatar
}