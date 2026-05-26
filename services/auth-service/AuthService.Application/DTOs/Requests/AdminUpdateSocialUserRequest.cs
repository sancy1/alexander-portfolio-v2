namespace AuthService.Application.DTOs.Requests;

public class AdminUpdateSocialUserRequest
{
    public string? DisplayName { get; set; }
    public bool? IsActive { get; set; }
}