namespace AuthService.Application.DTOs.Requests;

public class RestoreAccountRequest
{
    public string Username { get; set; } = string.Empty;
    public string AdminKey { get; set; } = string.Empty;
}