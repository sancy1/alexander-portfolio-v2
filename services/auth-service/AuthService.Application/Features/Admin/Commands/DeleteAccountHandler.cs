using MediatR;
using AuthService.Application.DTOs.Responses;
using AuthService.Application.Interfaces.Persistence;
using AuthService.Application.Interfaces.Services;
using AuthService.Application.Common;

namespace AuthService.Application.Features.Admin.Commands;

public class DeleteAccountHandler : IRequestHandler<DeleteAccountCommand, DeleteAccountResponse>
{
    private readonly IAdminRepository _adminRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICloudinaryService _cloudinaryService;
    private readonly IOutboxRepository _outboxRepository;

    public DeleteAccountHandler(
        IAdminRepository adminRepository,
        IUnitOfWork unitOfWork,
        ICloudinaryService cloudinaryService,
        IOutboxRepository outboxRepository)
    {
        _adminRepository = adminRepository;
        _unitOfWork = unitOfWork;
        _cloudinaryService = cloudinaryService;
        _outboxRepository = outboxRepository;
    }

    public async Task<DeleteAccountResponse> Handle(DeleteAccountCommand request, CancellationToken cancellationToken)
    {
        var admin = await _adminRepository.GetByIdAsync(request.AdminId);
        
        if (admin == null)
        {
            return new DeleteAccountResponse { Success = false, Message = "Admin not found" };
        }

        // Verify username matches
        if (admin.Username != request.ConfirmUsername)
        {
            return new DeleteAccountResponse { Success = false, Message = "Username confirmation does not match" };
        }

        if (request.PermanentDelete)
        {
            // Log account deletion event
            await OutboxHelper.AddToOutboxAsync(
                _outboxRepository,
                _unitOfWork,
                "account.deleted",
                "account.deleted",
                "kafka",
                new
                {
                    eventType = "account.deleted",
                    accountId = admin.Id,
                    accountType = "admin",
                    deleteType = "permanent",
                    username = admin.Username,
                    email = admin.Email,
                    reason = request.Reason,
                    timestamp = DateTime.UtcNow,
                    severity = "High"
                });
            
            // Delete avatar from Cloudinary if exists
            if (!string.IsNullOrEmpty(admin.AvatarUrl))
            {
                try
                {
                    var publicId = ExtractPublicId(admin.AvatarUrl);
                    await _cloudinaryService.DeleteImageAsync(publicId);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to delete avatar: {ex.Message}");
                }
            }

            _adminRepository.Delete(admin);
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
            // Log soft deletion
            await OutboxHelper.AddToOutboxAsync(
                _outboxRepository,
                _unitOfWork,
                "account.soft_deleted",
                "account.soft_deleted",
                "kafka",
                new
                {
                    eventType = "account.soft_deleted",
                    accountId = admin.Id,
                    accountType = "admin",
                    username = admin.Username,
                    email = admin.Email,
                    reason = request.Reason,
                    permanentDeleteDate = DateTime.UtcNow.AddDays(30),
                    timestamp = DateTime.UtcNow,
                    severity = "Medium"
                });
            
            admin.SoftDelete(request.Reason ?? "User requested deletion");
            _adminRepository.Update(admin);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new DeleteAccountResponse
            {
                Success = true,
                Message = "Account scheduled for permanent deletion in 30 days. You can restore your account anytime before then.",
                PermanentDeleteDate = admin.PermanentDeleteAt,
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