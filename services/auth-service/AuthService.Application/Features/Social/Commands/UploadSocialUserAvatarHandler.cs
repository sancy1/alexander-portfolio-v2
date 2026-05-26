// File: AuthService.Application/Features/Social/Commands/UploadSocialUserAvatarHandler.cs
// Purpose: Handles social user avatar upload to Cloudinary
// Layer: Application

using MediatR;
using AuthService.Application.Interfaces.Persistence;
using AuthService.Application.Interfaces.Services;

namespace AuthService.Application.Features.Social.Commands;

public class UploadSocialUserAvatarHandler : IRequestHandler<UploadSocialUserAvatarCommand, string?>
{
    private readonly ISocialUserRepository _socialUserRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICloudinaryService _cloudinaryService;

    public UploadSocialUserAvatarHandler(
        ISocialUserRepository socialUserRepository,
        IUnitOfWork unitOfWork,
        ICloudinaryService cloudinaryService)
    {
        _socialUserRepository = socialUserRepository;
        _unitOfWork = unitOfWork;
        _cloudinaryService = cloudinaryService;
    }

    public async Task<string?> Handle(UploadSocialUserAvatarCommand request, CancellationToken cancellationToken)
    {
        var user = await _socialUserRepository.GetByIdAsync(request.UserId);
        
        if (user == null)
        {
            return null;
        }

        // Delete old avatar from Cloudinary if exists and not default
        if (!string.IsNullOrEmpty(user.AvatarUrl) && !user.AvatarUrl.Contains("googleusercontent") && !user.AvatarUrl.Contains("github"))
        {
            try
            {
                var publicId = ExtractPublicId(user.AvatarUrl);
                await _cloudinaryService.DeleteImageAsync(publicId);
            }
            catch
            {
                // Continue even if delete fails
            }
        }

        // Upload new avatar
        var avatarUrl = await _cloudinaryService.UploadImageAsync(request.Avatar, "social_user_avatars");
        
        user.UpdateAvatar(avatarUrl);
        _socialUserRepository.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return avatarUrl;
    }

    private string ExtractPublicId(string avatarUrl)
    {
        var parts = avatarUrl.Split('/');
        var filename = parts[^1];
        var folder = parts[^2];
        return $"{folder}/{filename.Split('.')[0]}";
    }
}