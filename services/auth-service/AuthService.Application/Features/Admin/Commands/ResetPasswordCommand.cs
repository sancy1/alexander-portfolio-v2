using MediatR;

namespace AuthService.Application.Features.Admin.Commands;

public class ResetPasswordCommand : IRequest<bool>
{
    public string Username { get; set; }
    public string AdminKey { get; set; }
    public string NewPassword { get; set; }

    public ResetPasswordCommand(string username, string adminKey, string newPassword)
    {
        Username = username;
        AdminKey = adminKey;
        NewPassword = newPassword;
    }
}