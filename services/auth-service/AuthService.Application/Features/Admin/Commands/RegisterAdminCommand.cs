// File: AuthService.Application/Features/Admin/Commands/RegisterAdminCommand.cs
// Purpose: Command for registering a new admin
// Layer: Application

using MediatR;
using AuthService.Application.DTOs.Responses;

namespace AuthService.Application.Features.Admin.Commands;

public class RegisterAdminCommand : IRequest<AuthResponse>
{
    public string Username { get; set; }
    public string Email { get; set; }
    public string Password { get; set; }
    public string AdminKey { get; set; }

    public RegisterAdminCommand(string username, string email, string password, string adminKey)
    {
        Username = username;
        Email = email;
        Password = password;
        AdminKey = adminKey;
    }
}