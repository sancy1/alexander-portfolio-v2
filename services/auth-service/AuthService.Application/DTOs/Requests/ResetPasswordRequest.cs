namespace AuthService.Application.DTOs.Requests;

public class ResetPasswordRequest
{
    public string Username { get; set; } = string.Empty;
    public string AdminKey { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
    public string ConfirmNewPassword { get; set; } = string.Empty;
}