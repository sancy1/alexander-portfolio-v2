using MediatR;

namespace AuthService.Application.Features.Admin.Queries;

public class GetAdminByUsernameQuery : IRequest<global::AuthService.Domain.Entities.Admin?>
{
    public string Username { get; set; }

    public GetAdminByUsernameQuery(string username)
    {
        Username = username;
    }
}