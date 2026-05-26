using MediatR;

namespace AuthService.Application.Features.Admin.Commands;

public class RestoreAccountCommand : IRequest<bool>
{
    public string Username { get; set; }
    public string AdminKey { get; set; }

    public RestoreAccountCommand(string username, string adminKey)
    {
        Username = username;
        AdminKey = adminKey;
    }
}