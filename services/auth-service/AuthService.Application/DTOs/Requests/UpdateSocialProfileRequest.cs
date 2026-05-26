// File: AuthService.Application/DTOs/Requests/UpdateSocialProfileRequest.cs
// Purpose: Request DTO for updating social user profile
// Layer: Application

namespace AuthService.Application.DTOs.Requests;

public class UpdateSocialProfileRequest
{
    public string DisplayName { get; set; } = string.Empty;
}