// File: AuthService.Application/Features/Social/Commands/UploadSocialUserAvatarCommand.cs
// Purpose: Command for uploading social user avatar
// Layer: Application

using MediatR;
using Microsoft.AspNetCore.Http;

namespace AuthService.Application.Features.Social.Commands;

public class UploadSocialUserAvatarCommand : IRequest<string?>
{
    public Guid UserId { get; set; }
    public IFormFile Avatar { get; set; }

    public UploadSocialUserAvatarCommand(Guid userId, IFormFile avatar)
    {
        UserId = userId;
        Avatar = avatar;
    }
}