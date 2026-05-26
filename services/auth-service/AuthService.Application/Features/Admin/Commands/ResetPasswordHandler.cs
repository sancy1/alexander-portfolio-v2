using MediatR;
using AuthService.Application.Interfaces.Persistence;
using AuthService.Application.Interfaces.Security;
using AuthService.Domain.Interfaces;
using AuthService.Application.Common;

namespace AuthService.Application.Features.Admin.Commands;

public class ResetPasswordHandler : IRequestHandler<ResetPasswordCommand, bool>
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
        // 1. Validate admin key
        if (!_adminKeyValidator.IsValidAdminKey(request.AdminKey))
        {
            // Log failed password reset attempt
            await OutboxHelper.AddToOutboxAsync(
                _outboxRepository,
                _unitOfWork,
                "security.failed_password_reset",
                "security.failed_password_reset",
                "kafka",
                new
                {
                    eventType = "security.failed_password_reset",
                    username = request.Username,
                    timestamp = DateTime.UtcNow,
                    reason = "invalid_admin_key",
                    severity = "High"
                });
            return false;
        }

        // 2. Find admin by username
        var admin = await _adminRepository.GetByUsernameAsync(request.Username);
        
        if (admin == null)
        {
            // Log failed password reset attempt
            await OutboxHelper.AddToOutboxAsync(
                _outboxRepository,
                _unitOfWork,
                "security.failed_password_reset",
                "security.failed_password_reset",
                "kafka",
                new
                {
                    eventType = "security.failed_password_reset",
                    username = request.Username,
                    timestamp = DateTime.UtcNow,
                    reason = "user_not_found",
                    severity = "High"
                });
            return false;
        }

        // 3. Hash new password and update
        var newPasswordHash = _passwordHasher.HashPassword(request.NewPassword);
        admin.ChangePassword(newPasswordHash);
        
        _adminRepository.Update(admin);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Log successful password reset
        await OutboxHelper.AddToOutboxAsync(
            _outboxRepository,
            _unitOfWork,
            "security.password_reset",
            "security.password_reset",
            "kafka",
            new
            {
                eventType = "security.password_reset",
                adminId = admin.Id,
                username = admin.Username,
                email = admin.Email,
                timestamp = DateTime.UtcNow,
                severity = "High"
            });

        return true;
    }
}