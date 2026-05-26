namespace AuthService.Application.DTOs.Responses;

public class UpdateAdminProfileResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? Username { get; set; }
    public string? Email { get; set; }
}