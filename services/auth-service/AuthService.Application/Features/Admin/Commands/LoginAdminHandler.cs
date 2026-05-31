// File: AuthService.Application/Features/Admin/Commands/LoginAdminHandler.cs

// File: AuthService.Application/Features/Admin/Commands/LoginAdminHandler.cs
using MediatR;
using AuthService.Application.DTOs.Responses;
using AuthService.Application.DTOs.Events;
using AuthService.Domain.Enums;
using AuthService.Domain.Interfaces;
using AuthService.Application.Interfaces.Persistence;
using AuthService.Application.Common;
using AdminEntity = AuthService.Domain.Entities.Admin;

namespace AuthService.Application.Features.Admin.Commands;

public sealed class LoginAdminHandler : IRequestHandler<LoginAdminCommand, AdminLoginResponse>
{
    private readonly IAdminRepository _adminRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtGenerator _jwtGenerator;
    private readonly IOutboxRepository _outboxRepository;
    private readonly IUnitOfWork _unitOfWork;

    public LoginAdminHandler(
        IAdminRepository adminRepository,
        IPasswordHasher passwordHasher,
        IJwtGenerator jwtGenerator,
        IOutboxRepository outboxRepository,
        IUnitOfWork unitOfWork)
    {
        _adminRepository = adminRepository;
        _passwordHasher = passwordHasher;
        _jwtGenerator = jwtGenerator;
        _outboxRepository = outboxRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<AdminLoginResponse> Handle(
        LoginAdminCommand request,
        CancellationToken cancellationToken)
    {
        AdminEntity? admin = null;

        if (request.Username.Contains("@"))
            admin = await _adminRepository.GetByEmailAsync(request.Username);
        else
            admin = await _adminRepository.GetByUsernameAsync(request.Username);

        // ====================================================================
        // SECURITY PATHWAY A: ACCOUNT DELETED
        // ====================================================================
        if (admin != null && admin.IsDeleted)
        {
            var failedDeletedPayload = new
            {
                eventType = "security.failed_login",
                username = request.Username,
                occurredAt = DateTime.UtcNow,
                reason = "account_deleted",
                userType = "Admin",
                clientIp = request.ClientIp,
                userAgent = request.UserAgent,
                severity = "Medium"
            };

            // Post Office Notification Task
            await OutboxHelper.AddToOutboxAsync(_outboxRepository, "security.failed_login", "security.failed_login", "rabbitmq", failedDeletedPayload);
            
            // Permanent Security Archive Log Entry
            await OutboxHelper.AddToOutboxAsync(_outboxRepository, "security-audit-logs", "security.failed_login", "kafka", failedDeletedPayload);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return AdminLoginResponse.CreateFailure("Account has been deleted. Please restore your account first.");
        }

        // ====================================================================
        // SECURITY PATHWAY B: ACCOUNT DEACTIVATED
        // ====================================================================
        if (admin != null && admin.Status == AccountStatus.Deactivated)
        {
            var failedDeactivatedPayload = new
            {
                eventType = "security.failed_login",
                username = request.Username,
                occurredAt = DateTime.UtcNow,
                reason = "account_deactivated",
                userType = "Admin",
                clientIp = request.ClientIp,
                userAgent = request.UserAgent,
                severity = "Medium"
            };

            // Post Office Notification Task
            await OutboxHelper.AddToOutboxAsync(_outboxRepository, "security.failed_login", "security.failed_login", "rabbitmq", failedDeactivatedPayload);
            
            // Permanent Security Archive Log Entry
            await OutboxHelper.AddToOutboxAsync(_outboxRepository, "security-audit-logs", "security.failed_login", "kafka", failedDeactivatedPayload);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return AdminLoginResponse.CreateFailure("Account is deactivated. Contact support.");
        }

        // ====================================================================
        // SECURITY PATHWAY C: INVALID CREDENTIALS
        // ====================================================================
        if (admin == null || !_passwordHasher.VerifyPassword(request.Password, admin.PasswordHash))
        {
            var invalidCredentialsPayload = new
            {
                eventType = "security.failed_login",
                username = request.Username,
                occurredAt = DateTime.UtcNow,
                reason = admin == null ? "user_not_found" : "invalid_password",
                userType = "Admin",
                clientIp = request.ClientIp,
                userAgent = request.UserAgent,
                severity = "Medium"
            };

            // Post Office Notification Task
            await OutboxHelper.AddToOutboxAsync(_outboxRepository, "security.failed_login", "security.failed_login", "rabbitmq", invalidCredentialsPayload);
            
            // Permanent Security Archive Log Entry
            await OutboxHelper.AddToOutboxAsync(_outboxRepository, "security-audit-logs", "security.failed_login", "kafka", invalidCredentialsPayload);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return AdminLoginResponse.CreateFailure("Invalid username/email or password");
        }

        // ====================================================================
        // SUCCESS PATHWAY - Dual-write transaction to Outbox for both brokers
        // ====================================================================
        admin.RecordLogin();
        _adminRepository.Update(admin);

        var loginEvent = new UserLoggedInEvent
        {
            EventType = "admin.loggedin",
            OccurredAt = DateTime.UtcNow,
            UserId = admin.Id,
            Email = admin.Email,
            UserType = "Admin",
            LoginMethod = "password",
            ClientIp = request.ClientIp,
            UserAgent = request.UserAgent
        };

        // Write to Outbox for RabbitMQ (Real-time microservice broadcast tasks)
        await OutboxHelper.AddToOutboxAsync(_outboxRepository, "admin.loggedin", "admin.loggedin", "rabbitmq", loginEvent);

        // Write to Outbox for Kafka (Permanent Chronological Timeline Log)
        await OutboxHelper.AddToOutboxAsync(_outboxRepository, "auth-events", "admin.loggedin", "kafka", loginEvent);

        // Commit all entities and both outbox message logs in one atomic database push
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var token = _jwtGenerator.GenerateAdminToken(admin);
        return AdminLoginResponse.CreateSuccess(token, admin.Id, admin.Username, admin.Email);
    }
}
