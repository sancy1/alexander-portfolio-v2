// File: AuthService.Application/Features/Admin/Commands/DeleteAccountHandler.cs
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;
using AuthService.Application.DTOs.Responses;
using AuthService.Application.Interfaces.Persistence;
using AuthService.Application.Interfaces.Services;
using AuthService.Application.Common;

namespace AuthService.Application.Features.Admin.Commands;

public sealed class DeleteAccountHandler : IRequestHandler<DeleteAccountCommand, DeleteAccountResponse>
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

        // Keep a snapshot of the image URL locally before mutating or removing the entity context
        var avatarUrlSnapshot = admin.AvatarUrl;

        // 💾 Claim Check Rule: Track lightweight metadata primitives (~200 bytes)
        var deletionEvent = new
        {
            eventType = request.PermanentDelete ? "account.permanently_deleted" : "account.soft_deleted",
            accountId = admin.Id,
            accountType = "admin",
            username = admin.Username,
            email = admin.Email,
            reason = request.Reason,
            timestamp = DateTime.UtcNow,
            severity = request.PermanentDelete ? "Critical" : "High"
        };

        // ====================================================================
        // BRANCH A: PERMANENT DESTRUCTION PIPELINE
        // ====================================================================
        if (request.PermanentDelete)
        {
            // Write to Outbox for Kafka (Permanent Chronological Timeline Log)
            await OutboxHelper.AddToOutboxAsync(
                _outboxRepository,
                "security-audit-logs",
                "account.permanently_deleted",
                "kafka",
                deletionEvent);

            // Notify RabbitMQ so other microservices can completely purge associated user records
            await OutboxHelper.AddToOutboxAsync(
                _outboxRepository,
                "account.permanently_deleted",
                "account.permanently_deleted",
                "rabbitmq",
                deletionEvent);

            // Queue entity deletion in the DbContext tracking log
            _adminRepository.Delete(admin);

            // 🏗️ Rules Applied: SINGLE ATOMIC TRANSACTION COMMIT
            // This ensures outbox rows and the database purge succeed or fail as a single unit.
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // ☁️ Execute external media cleanup outside the critical database transaction block
            if (!string.IsNullOrEmpty(avatarUrlSnapshot))
            {
                string publicId = string.Empty;
                try
                {
                    publicId = ExtractPublicId(avatarUrlSnapshot);
                    await _cloudinaryService.DeleteImageAsync(publicId);
                }
                catch (Exception ex)
                {
                    // Log internally but do not crash - database integrity is already secure
                    Console.WriteLine($"Non-critical infrastructure failure: Cloudinary image {publicId} cleanup skipped: {ex.Message}");
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
        // BRANCH B: SOFT REVERSIBLE DELETION PIPELINE
        // ====================================================================
        else
        {
            // Write to Outbox for RabbitMQ (Real-time notification engine)
            await OutboxHelper.AddToOutboxAsync(
                _outboxRepository,
                "account.soft_deleted",
                "account.soft_deleted",
                "rabbitmq",
                deletionEvent);

            // Write to Outbox for Kafka (Permanent compliance logging)
            await OutboxHelper.AddToOutboxAsync(
                _outboxRepository,
                "security-audit-logs",
                "account.soft_deleted",
                "kafka",
                deletionEvent);
            
            admin.SoftDelete(request.Reason ?? "User requested deletion");
            _adminRepository.Update(admin);

            // Single unified atomic transactional push
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

    private static string ExtractPublicId(string avatarUrl)
    {
        var parts = avatarUrl.Split('/');
        var filename = parts[^1];
        var folder = parts[^2];
        return $"{folder}/{filename.Split('.')[0]}";
    }
}
