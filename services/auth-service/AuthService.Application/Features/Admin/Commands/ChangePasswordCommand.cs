using MediatR;

namespace AuthService.Application.Features.Admin.Commands;

public class ChangePasswordCommand : IRequest<bool>
{
    public Guid AdminId { get; set; }
    public string CurrentPassword { get; set; }
    public string NewPassword { get; set; }

    public ChangePasswordCommand(Guid adminId, string currentPassword, string newPassword)
    {
        AdminId = adminId;
        CurrentPassword = currentPassword;
        NewPassword = newPassword;
    }
}