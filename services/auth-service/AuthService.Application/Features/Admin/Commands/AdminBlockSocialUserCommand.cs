using MediatR;

namespace AuthService.Application.Features.Admin.Commands;

public class AdminBlockSocialUserCommand : IRequest<bool>
{
    public Guid UserId { get; set; }
    public string Reason { get; set; }

    public AdminBlockSocialUserCommand(Guid userId, string reason)
    {
        UserId = userId;
        Reason = reason;
    }
}