// File: AuthService.Application/Features/Admin/Commands/LoginAdminHandler.cs
using MediatR;
using AuthService.Application.DTOs.Responses;
using AuthService.Application.DTOs.Events;
using AuthService.Domain.Enums;
using AuthService.Domain.Interfaces;
using AuthService.Application.Interfaces.Persistence;
using AuthService.Application.Common;
using AdminEntity = AuthService.Domain.Entities.Admin;  // ← alias avoids namespace conflict

namespace AuthService.Application.Features.Admin.Commands;

public class LoginAdminHandler : IRequestHandler<LoginAdminCommand, AdminLoginResponse>
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
        AdminEntity? admin = null;   // ← use alias

        if (request.Username.Contains("@"))
            admin = await _adminRepository.GetByEmailAsync(request.Username);
        else
            admin = await _adminRepository.GetByUsernameAsync(request.Username);

        // ====================================================================
        // SECURITY PATHWAY A: ACCOUNT DELETED
        // ====================================================================
        if (admin != null && admin.IsDeleted)
        {
            await OutboxHelper.AddToOutboxAsync(
                _outboxRepository,
                "security.failed_login",
                "security.failed_login",
                "rabbitmq",
                new
                {
                    eventType = "security.failed_login",
                    username = request.Username,
                    occurredAt = DateTime.UtcNow,
                    reason = "account_deleted",
                    userType = "Admin",
                    clientIp = request.ClientIp,
                    userAgent = request.UserAgent,
                    severity = "Medium"
                });

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return AdminLoginResponse.CreateFailure(
                "Account has been deleted. Please restore your account first.");
        }

        // ====================================================================
        // SECURITY PATHWAY B: ACCOUNT DEACTIVATED
        // ====================================================================
        if (admin != null && admin.Status == AccountStatus.Deactivated)
        {
            await OutboxHelper.AddToOutboxAsync(
                _outboxRepository,
                "security.failed_login",
                "security.failed_login",
                "rabbitmq",
                new
                {
                    eventType = "security.failed_login",
                    username = request.Username,
                    occurredAt = DateTime.UtcNow,
                    reason = "account_deactivated",
                    userType = "Admin",
                    clientIp = request.ClientIp,
                    userAgent = request.UserAgent,
                    severity = "Medium"
                });

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return AdminLoginResponse.CreateFailure("Account is deactivated. Contact support.");
        }

        // ====================================================================
        // SECURITY PATHWAY C: INVALID CREDENTIALS
        // ====================================================================
        if (admin == null || !_passwordHasher.VerifyPassword(request.Password, admin.PasswordHash))
        {
            await OutboxHelper.AddToOutboxAsync(
                _outboxRepository,
                "security.failed_login",
                "security.failed_login",
                "rabbitmq",
                new
                {
                    eventType = "security.failed_login",
                    username = request.Username,
                    occurredAt = DateTime.UtcNow,
                    reason = admin == null ? "user_not_found" : "invalid_password",
                    userType = "Admin",
                    clientIp = request.ClientIp,
                    userAgent = request.UserAgent,
                    severity = "Medium"
                });

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return AdminLoginResponse.CreateFailure("Invalid username/email or password");
        }

        // ====================================================================
        // SUCCESS PATHWAY
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

        await OutboxHelper.AddToOutboxAsync(
            _outboxRepository,
            "admin.loggedin",
            "admin.loggedin",
            "rabbitmq",
            loginEvent
        );

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var token = _jwtGenerator.GenerateAdminToken(admin);
        return AdminLoginResponse.CreateSuccess(
            token, admin.Id, admin.Username, admin.Email);
    }
}