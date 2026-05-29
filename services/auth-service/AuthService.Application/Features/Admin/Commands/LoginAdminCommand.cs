// File: AuthService.Application/Features/Admin/Commands/LoginAdminCommand.cs
using MediatR;
using AuthService.Application.DTOs.Responses;

namespace AuthService.Application.Features.Admin.Commands;

public class LoginAdminCommand : IRequest<AdminLoginResponse>
{
    public string Username { get; }
    public string Password { get; }
    
    // 👇 Context items appended to carry tracking details down from Nginx/Controllers
    public string ClientIp { get; }
    public string UserAgent { get; }

    public LoginAdminCommand(string username, string password, string clientIp, string userAgent)
    {
        Username = username;
        Password = password;
        ClientIp = string.IsNullOrEmpty(clientIp) ? "Unknown" : clientIp;
        UserAgent = string.IsNullOrEmpty(userAgent) ? "Unknown" : userAgent;
    }
}
