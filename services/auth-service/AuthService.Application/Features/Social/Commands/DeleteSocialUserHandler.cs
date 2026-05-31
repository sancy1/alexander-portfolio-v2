// File: AuthService.Application/Features/Social/Commands/DeleteSocialUserHandler.cs
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;
using AuthService.Application.DTOs.Responses;
using AuthService.Application.Interfaces.Persistence;
using AuthService.Application.Interfaces.Services;
using AuthService.Application.Common;

namespace AuthService.Application.Features.Social.Commands;

public sealed class DeleteSocialUserHandler : IRequestHandler<DeleteSocialUserCommand, DeleteAccountResponse>
{
    private readonly ISocialUserRepository _socialUserRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICloudinaryService _cloudinaryService;
    private readonly IOutboxRepository _outboxRepository;

    public DeleteSocialUserHandler(
        ISocialUserRepository socialUserRepository,
        IUnitOfWork unitOfWork,
        ICloudinaryService cloudinaryService,
        IOutboxRepository outboxRepository)
    {
        _socialUserRepository = socialUserRepository;
        _unitOfWork = unitOfWork;
        _cloudinaryService = cloudinaryService;
        _outboxRepository = outboxRepository;
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

        // Cache the avatar URL metadata snapshot prior to state mutations
        var avatarUrlSnapshot = user.AvatarUrl;

        // 💾 Claim Check Rule: Track lightweight metadata primitives (~200 bytes)
        var deletionEvent = new
        {
            eventType = request.PermanentDelete ? "social.account.permanently_deleted" : "social.account.soft_deleted",
            accountId = user.Id,
            accountType = "social_user",
            email = user.Email,
            displayName = user.DisplayName,
            provider = user.Provider.ToString(),
            reason = request.Reason,
            timestamp = DateTime.UtcNow,
            severity = request.PermanentDelete ? "Critical" : "High"
        };

        // ====================================================================
        // BRANCH A: PERMANENT PURGE PIPELINE
        // ====================================================================
        if (request.PermanentDelete)
        {
            // 🗄️ Big Archive: Log chronological timeline entry to disk permanently
            await OutboxHelper.AddToOutboxAsync(
                _outboxRepository,
                "security-audit-logs",
                "social.account.permanently_deleted",
                "kafka",
                deletionEvent);

            // 📬 Post Office: Broadcast deletion event so other microservices drop user references
            await OutboxHelper.AddToOutboxAsync(
                _outboxRepository,
                "social.account.permanently_deleted",
                "social.account.permanently_deleted",
                "rabbitmq",
                deletionEvent);
            
            // Queue destruction inside the EF Core entity tracker
            _socialUserRepository.Delete(user);
            
            // 🏗️ Rules Applied: SINGLE ATOMIC TRANSACTION COMMIT
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            
            // ☁️ Execute external media cleanup outside the critical database transaction block
            if (!string.IsNullOrEmpty(avatarUrlSnapshot) && 
                !avatarUrlSnapshot.Contains("googleusercontent", StringComparison.OrdinalIgnoreCase) && 
                !avatarUrlSnapshot.Contains("github", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    var publicId = ExtractPublicId(avatarUrlSnapshot);
                    await _cloudinaryService.DeleteImageAsync(publicId);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Non-critical infrastructure failure: Cloudinary cleanup skipped: {ex.Message}");
                }
            }

            return new DeleteAccountResponse
            {
                Success = true,
                Message = "Account permanently deleted. This action cannot be undone.",
                IsReversible = false
            };
        }
        // ====================================================================
        // BRANCH B: SOFT REVERSIBLE PIPELINE
        // ====================================================================
        else
        {
            // 📬 Post Office Channel
            await OutboxHelper.AddToOutboxAsync(
                _outboxRepository,
                "social.account.soft_deleted",
                "social.account.soft_deleted",
                "rabbitmq",
                deletionEvent);

            // 🗄️ Big Archive Channel
            await OutboxHelper.AddToOutboxAsync(
                _outboxRepository,
                "security-audit-logs",
                "social.account.soft_deleted",
                "kafka",
                deletionEvent);
            
            user.SoftDelete(request.Reason ?? "User requested deletion");
            _socialUserRepository.Update(user);

            // Singular atomic transactional push for soft deletion
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

    private static string ExtractPublicId(string avatarUrl)
    {
        var parts = avatarUrl.Split('/');
        var filename = parts[^1];
        var folder = parts[^2];
        return $"{folder}/{filename.Split('.')[0]}";
    }
}
