namespace AuthService.Application.DTOs.Requests;

public class UpdateAdminProfileRequest
{
    public string? Username { get; set; }
    public string? Email { get; set; }
}