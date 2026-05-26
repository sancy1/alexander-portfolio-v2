using Microsoft.AspNetCore.Http;

namespace AuthService.Application.DTOs.Requests;

public class AvatarUploadRequest
{
    public IFormFile? Avatar { get; set; }
}