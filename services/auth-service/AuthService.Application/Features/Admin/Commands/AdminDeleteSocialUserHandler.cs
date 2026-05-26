// File: AuthService.Application/Features/Admin/Commands/AdminDeleteSocialUserHandler.cs
// Purpose: Handles admin forced deletion of social user accounts
// Layer: Application

using MediatR;
using AuthService.Application.DTOs.Responses;
using AuthService.Application.Interfaces.Persistence;
using AuthService.Application.Interfaces.Services;

namespace AuthService.Application.Features.Admin.Commands;

public class AdminDeleteSocialUserHandler : IRequestHandler<AdminDeleteSocialUserCommand, DeleteAccountResponse>
{
    private readonly ISocialUserRepository _socialUserRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICloudinaryService _cloudinaryService;

    public AdminDeleteSocialUserHandler(
        ISocialUserRepository socialUserRepository,
        IUnitOfWork unitOfWork,
        ICloudinaryService cloudinaryService)
    {
        _socialUserRepository = socialUserRepository;
        _unitOfWork = unitOfWork;
        _cloudinaryService = cloudinaryService;
    }

    public async Task<DeleteAccountResponse> Handle(AdminDeleteSocialUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _socialUserRepository.GetByIdAsync(request.UserId);
        
        if (user == null)
        {
            return new DeleteAccountResponse { Success = false, Message = "User not found" };
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
                Message = $"User {user.Email} has been permanently deleted. Action: {request.Reason}",
                IsReversible = false
            };
        }
        else
        {
            // SOFT DELETE - Admin forced deletion
            user.SoftDelete($"Admin action: {request.Reason}");
            _socialUserRepository.Update(user);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new DeleteAccountResponse
            {
                Success = true,
                Message = $"User {user.Email} has been deactivated. Action: {request.Reason}",
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