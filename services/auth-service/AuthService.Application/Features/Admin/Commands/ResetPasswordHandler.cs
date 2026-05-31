// File: AuthService.Application/Features/Admin/Commands/ResetPasswordHandler.cs
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;
using AuthService.Application.Interfaces.Persistence;
using AuthService.Application.Interfaces.Security;
using AuthService.Domain.Interfaces;
using AuthService.Application.Common;

namespace AuthService.Application.Features.Admin.Commands;

public sealed class ResetPasswordHandler : IRequestHandler<ResetPasswordCommand, bool>
{
    private readonly IAdminRepository _adminRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IAdminKeyValidator _adminKeyValidator;
    private readonly IOutboxRepository _outboxRepository;

    public ResetPasswordHandler(
        IAdminRepository adminRepository,
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        IAdminKeyValidator adminKeyValidator,
        IOutboxRepository outboxRepository)
    {
        _adminRepository = adminRepository;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _adminKeyValidator = adminKeyValidator;
        _outboxRepository = outboxRepository;
    }

    public async Task<bool> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        // ====================================================================
        // EXCEPTION PATHWAY A: SECURITY KEY VIOLATION
        // ====================================================================
        if (!_adminKeyValidator.IsValidAdminKey(request.AdminKey))
        {
            var failedResetEvent = new
            {
                eventType = "security.failed_password_reset",
                username = request.Username,
                timestamp = DateTime.UtcNow,
                reason = "invalid_admin_key",
                severity = "High"
            };

            // Write to Outbox for Kafka only (security audit)
            await OutboxHelper.AddToOutboxAsync(
                _outboxRepository,
                "security-audit-logs",
                "security.failed_password_reset",
                "kafka",
                failedResetEvent);
                
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return false;
        }

        // ====================================================================
        // EXCEPTION PATHWAY B: TARGET USER NOT FOUND
        // ====================================================================
        var admin = await _adminRepository.GetByUsernameAsync(request.Username);
        if (admin == null)
        {
            var failedResetEvent = new
            {
                eventType = "security.failed_password_reset",
                username = request.Username,
                timestamp = DateTime.UtcNow,
                reason = "user_not_found",
                severity = "High"
            };

            // Write to Outbox for Kafka only (security audit)
            await OutboxHelper.AddToOutboxAsync(
                _outboxRepository,
                "security-audit-logs",
                "security.failed_password_reset",
                "kafka",
                failedResetEvent);
                
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return false;
        }

        // ====================================================================
        // PATHWAY C: SUCCESSFUL SYSTEM STATE MUTATION
        // ====================================================================
        // 1. Transform password values inside local execution space memory
        var newPasswordHash = _passwordHasher.HashPassword(request.NewPassword);
        admin.ChangePassword(newPasswordHash);
        _adminRepository.Update(admin);

        // 2. Draft the ~200 byte lightweight reference tracking metadata payload
        var successResetEvent = new
        {
            eventType = "security.password_reset",
            adminId = admin.Id,
            username = admin.Username,
            email = admin.Email,
            timestamp = DateTime.UtcNow,
            severity = "High"
        };

        // 📬 Post Office Layer: Deliver real-time synchronization alerts to surrounding applications
        await OutboxHelper.AddToOutboxAsync(
            _outboxRepository,
            "security.password_reset",
            "security.password_reset",
            "rabbitmq",
            successResetEvent);

        // 🗄️ Big Archive Layer: Keep an indestructible ledger of high-severity actions
        await OutboxHelper.AddToOutboxAsync(
            _outboxRepository,
            "security-audit-logs", // Uniform security classification mapping
            "security.password_reset",
            "kafka",
            successResetEvent);

        // 🏗️ Rules Applied: SINGLE TRANSACTION COMMIT ENTRY POINT
        // Guarantees all data changes, task records, and log entries succeed or fail together.
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}
