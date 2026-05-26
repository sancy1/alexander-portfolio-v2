
// File: AuthService.Application/Features/Admin/Commands/LoginAdminCommand.cs
// Purpose: Command for admin login
// Layer: Application

using MediatR;
using AuthService.Application.DTOs.Responses;

namespace AuthService.Application.Features.Admin.Commands;

public class LoginAdminCommand : IRequest<AdminLoginResponse>
{
    public string Username { get; set; }
    public string Password { get; set; }

    public LoginAdminCommand(string username, string password)
    {
        Username = username;
        Password = password;
    }
}
