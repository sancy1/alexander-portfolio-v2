
// File: AuthService.Application/DTOs/Requests/AdminLoginRequest.cs
// Purpose: Request DTO for admin login
// Layer: Application

namespace AuthService.Application.DTOs.Requests;

public class AdminLoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
