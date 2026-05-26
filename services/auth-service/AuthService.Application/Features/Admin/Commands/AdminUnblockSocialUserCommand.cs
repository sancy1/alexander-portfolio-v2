using MediatR;

namespace AuthService.Application.Features.Admin.Commands;

public class AdminUnblockSocialUserCommand : IRequest<bool>
{
    public Guid UserId { get; set; }

    public AdminUnblockSocialUserCommand(Guid userId)
    {
        UserId = userId;
    }
}