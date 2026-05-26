// File: AuthService.Application/Features/Social/Commands/DeleteSocialUserHandler.cs
// Purpose: Handles social user self-deletion
// Layer: Application

using MediatR;
using AuthService.Application.DTOs.Responses;
using AuthService.Application.Interfaces.Persistence;
using AuthService.Application.Interfaces.Services;

namespace AuthService.Application.Features.Social.Commands;

public class DeleteSocialUserHandler : IRequestHandler<DeleteSocialUserCommand, DeleteAccountResponse>
{
    private readonly ISocialUserRepository _socialUserRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICloudinaryService _cloudinaryService;

    public DeleteSocialUserHandler(
        ISocialUserRepository socialUserRepository,
        IUnitOfWork unitOfWork,
        ICloudinaryService cloudinaryService)
    {
        _socialUserRepository = socialUserRepository;
        _unitOfWork = unitOfWork;
        _cloudinaryService = cloudinaryService;
    }

    public async Task<DeleteAccountResponse> Handle(DeleteSocialUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _socialUserRepository.GetByIdAsync(request.UserId);
        
        if (user == null)
        {
            return new DeleteAccountResponse { Success = false, Message = "User not found" };
        }

        // Verify email matches
        if (!user.Email.Equals(request.ConfirmEmail, StringComparison.OrdinalIgnoreCase))
        {
            return new DeleteAccountResponse { Success = false, Message = "Email confirmation does not match" };
        }

        if (request.PermanentDelete)
        {
            // HARD DELETE - Immediate, Permanent
            
            // Delete avatar from Cloudinary if exists (and not default social avatar)
            if (!string.IsNullOrEmpty(user.AvatarUrl) && 
                !user.AvatarUrl.Contains("googleusercontent") && 
                !user.AvatarUrl.Contains("github"))
            {
                try
                {
                    var publicId = ExtractPublicId(user.AvatarUrl);
                    await _cloudinaryService.DeleteImageAsync(publicId);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to delete avatar: {ex.Message}");
                }
            }

            // Permanently remove from database
            _socialUserRepository.Delete(user);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new DeleteAccountResponse
            {
                Success = true,
                Message = "Account permanently deleted. This action cannot be undone.",
                IsReversible = false
            };
        }
        else
        {
            // SOFT DELETE - 30 days reversible
            user.SoftDelete(request.Reason ?? "User requested deletion");
            _socialUserRepository.Update(user);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new DeleteAccountResponse
            {
                Success = true,
                Message = "Account scheduled for permanent deletion in 30 days. You can restore your account anytime before then by logging in again.",
                PermanentDeleteDate = user.PermanentDeleteAt,
                IsReversible = true
            };
        }
    }

    private string ExtractPublicId(string avatarUrl)
    {
        var parts = avatarUrl.Split('/');
        var filename = parts[^1];
        var folder = parts[^2];
        return $"{folder}/{filename.Split('.')[0]}";
    }
}