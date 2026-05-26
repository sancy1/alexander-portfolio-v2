// File: AuthService.Application/DTOs/Requests/AdminRegisterRequest.cs
// Purpose: Request DTO for admin registration
// Layer: Application

namespace AuthService.Application.DTOs.Requests;

public class AdminRegisterRequest
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
    public string AdminKey { get; set; } = string.Empty;
}