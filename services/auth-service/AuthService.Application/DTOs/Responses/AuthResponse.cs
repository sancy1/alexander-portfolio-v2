
// File: AuthService.Application/DTOs/Responses/AuthResponse.cs
// Purpose: Response DTO for authentication operations
// Layer: Application

namespace AuthService.Application.DTOs.Responses;

public class AuthResponse
{
    public bool Success { get; set; }
    public string? Token { get; set; }
    public Guid? UserId { get; set; }
    public string? Message { get; set; }
    public bool RequiresProfileCompletion { get; set; }

    public static AuthResponse CreateSuccess(string token, Guid userId)
    {
        return new AuthResponse
        {
            Success = true,
            Token = token,
            UserId = userId,
            RequiresProfileCompletion = false
        };
    }

    public static AuthResponse CreateFailure(string message)
    {
        return new AuthResponse
        {
            Success = false,
            Message = message,
            RequiresProfileCompletion = false
        };
    }

    public static AuthResponse CreateProfileIncomplete(Guid userId)
    {
        return new AuthResponse
        {
            Success = true,
            UserId = userId,
            RequiresProfileCompletion = true,
            Message = "Please complete your profile"
        };
    }
}
