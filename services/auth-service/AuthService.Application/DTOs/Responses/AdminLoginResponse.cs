
// File: AuthService.Application/DTOs/Responses/AdminLoginResponse.cs
// Purpose: Response DTO for admin login
// Layer: Application

namespace AuthService.Application.DTOs.Responses;

public class AdminLoginResponse
{
    public bool Success { get; set; }
    public string? Token { get; set; }
    public Guid? AdminId { get; set; }
    public string? Username { get; set; }
    public string? Email { get; set; }
    public string? Message { get; set; }

    public static AdminLoginResponse CreateSuccess(string token, Guid adminId, string username, string email)
    {
        return new AdminLoginResponse
        {
            Success = true,
            Token = token,
            AdminId = adminId,
            Username = username,
            Email = email
        };
    }

    public static AdminLoginResponse CreateFailure(string message)
    {
        return new AdminLoginResponse
        {
            Success = false,
            Message = message
        };
    }
}
