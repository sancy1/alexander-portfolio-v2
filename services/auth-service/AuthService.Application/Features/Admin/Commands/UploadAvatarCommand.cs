using MediatR;
using Microsoft.AspNetCore.Http;

namespace AuthService.Application.Features.Admin.Commands;

public class UploadAvatarCommand : IRequest<string?>
{
    public Guid AdminId { get; set; }
    public IFormFile? Avatar { get; set; }

    public UploadAvatarCommand(Guid adminId, IFormFile? avatar)
    {
        AdminId = adminId;
        Avatar = avatar;
    }
}