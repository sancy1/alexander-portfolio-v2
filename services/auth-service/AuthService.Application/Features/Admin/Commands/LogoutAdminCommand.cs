using MediatR;

namespace AuthService.Application.Features.Admin.Commands;

public class LogoutAdminCommand : IRequest<bool>
{
    public string Token { get; set; }

    public LogoutAdminCommand(string token)
    {
        Token = token;
    }
}