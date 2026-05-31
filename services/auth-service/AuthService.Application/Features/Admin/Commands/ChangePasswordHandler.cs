// File: AuthService.Application/Features/Admin/Commands/ChangePasswordHandler.cs
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;
using AuthService.Application.Interfaces.Persistence;
using AuthService.Domain.Interfaces;
using AuthService.Application.Common;

namespace AuthService.Application.Features.Admin.Commands;

public sealed class ChangePasswordHandler : IRequestHandler<ChangePasswordCommand, bool>
{
    private readonly IAdminRepository _adminRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IOutboxRepository _outboxRepository;

    public ChangePasswordHandler(
        IAdminRepository adminRepository,
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        IOutboxRepository outboxRepository)
    {
        _adminRepository = adminRepository;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _outboxRepository = outboxRepository;
    }

    public async Task<bool> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        var admin = await _adminRepository.GetByIdAsync(request.AdminId);
        
        if (admin == null)
        {
            return false;
        }

        // ====================================================================
        // PATHWAY A: SECURITY EXCEPTION (INVALID CURRENT PASSWORD)
        // ====================================================================
        if (!_passwordHasher.VerifyPassword(request.CurrentPassword, admin.PasswordHash))
        {
            // Log failed password change attempt to Kafka only (security audit log)
            await OutboxHelper.AddToOutboxAsync(
                _outboxRepository,
                "security-audit-logs", // Matches Kafka consumer security topic routing
                "security.failed_password_change",
                "kafka",
                new
                {
                    eventType = "security.failed_password_change",
                    adminId = admin.Id,
                    username = admin.Username,
                    timestamp = DateTime.UtcNow,
                    reason = "invalid_current_password",
                    severity = "Medium"
                });

            // Single atomic push for the exception path
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return false;
        }

        // ====================================================================
        // PATHWAY B: SUCCESSFUL TRANSACTION LAYER
        // ====================================================================
        // 1. Mutate state variables in-memory
        var newPasswordHash = _passwordHasher.HashPassword(request.NewPassword);
        admin.ChangePassword(newPasswordHash);
        _adminRepository.Update(admin);

        // 2. Build the ~200 byte Claim Check payload object
        var passwordChangeEvent = new
        {
            eventType = "admin.password_changed",
            adminId = admin.Id,
            username = admin.Username,
            timestamp = DateTime.UtcNow,
            severity = "Medium"
        };

        // 📬 Post Office: Broadcast real-time notifications to invalidate active sessions/tokens
        await OutboxHelper.AddToOutboxAsync(
            _outboxRepository,
            "admin.password_changed",
            "admin.password_changed",
            "rabbitmq",
            passwordChangeEvent);

        // 🗄️ Big Archive: Log the successful security mutation permanently to disk
        await OutboxHelper.AddToOutboxAsync(
            _outboxRepository,
            "security-audit-logs", // Uniform security topic grouping
            "admin.password_changed",
            "kafka",
            passwordChangeEvent);

        // 🏗️ Rules Applied: SINGLE ATOMIC TRANSACTION COMMIT
        // The admin update and both broker records are locked together inside PostgreSQL.
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}
