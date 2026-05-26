// File: AuthService.Application/Features/Admin/Commands/LoginAdminHandler.cs
// Purpose: Handles admin login logic with soft delete check and outbox audit logging
// Layer: Application

using MediatR;
using AuthService.Application.DTOs.Responses;
using AuthService.Domain.Entities;
using AuthService.Domain.Enums;
using AuthService.Domain.Interfaces;
using AuthService.Application.Interfaces.Persistence;
using AuthService.Application.Common;

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

    public async Task<AdminLoginResponse> Handle(LoginAdminCommand request, CancellationToken cancellationToken)
    {
        // Use global:: to avoid namespace conflict with Features.Admin
        global::AuthService.Domain.Entities.Admin? admin = null;
        
        if (request.Username.Contains("@"))
        {
            admin = await _adminRepository.GetByEmailAsync(request.Username);
        }
        else
        {
            admin = await _adminRepository.GetByUsernameAsync(request.Username);
        }

        // CHECK IF ACCOUNT IS SOFT DELETED
        if (admin != null && admin.IsDeleted)
        {
            // Log failed attempt - deleted account
            await OutboxHelper.AddToOutboxAsync(
                _outboxRepository,
                _unitOfWork,
                "security.failed_login",
                "security.failed_login",
                "kafka",
                new
                {
                    eventType = "security.failed_login",
                    username = request.Username,
                    timestamp = DateTime.UtcNow,
                    reason = "account_deleted",
                    severity = "Medium"
                });
            
            return AdminLoginResponse.CreateFailure("Account has been deleted. Please restore your account first.");
        }

        // CHECK IF ACCOUNT IS DEACTIVATED
        if (admin != null && admin.Status == AccountStatus.Deactivated)
        {
            await OutboxHelper.AddToOutboxAsync(
                _outboxRepository,
                _unitOfWork,
                "security.failed_login",
                "security.failed_login",
                "kafka",
                new
                {
                    eventType = "security.failed_login",
                    username = request.Username,
                    timestamp = DateTime.UtcNow,
                    reason = "account_deactivated",
                    severity = "Medium"
                });
            
            return AdminLoginResponse.CreateFailure("Account is deactivated. Contact support.");
        }

        // Check password
        if (admin == null || !_passwordHasher.VerifyPassword(request.Password, admin.PasswordHash))
        {
            // Log failed login attempt
            await OutboxHelper.AddToOutboxAsync(
                _outboxRepository,
                _unitOfWork,
                "security.failed_login",
                "security.failed_login",
                "kafka",
                new
                {
                    eventType = "security.failed_login",
                    username = request.Username,
                    timestamp = DateTime.UtcNow,
                    reason = admin == null ? "user_not_found" : "invalid_password",
                    severity = "Medium"
                });
            
            return AdminLoginResponse.CreateFailure("Invalid username/email or password");
        }

        admin.RecordLogin();
        _adminRepository.Update(admin);

        var token = _jwtGenerator.GenerateAdminToken(admin);

        // 1. RabbitMQ Message (for future services)
        await OutboxHelper.AddToOutboxAsync(
            _outboxRepository,
            _unitOfWork,
            "admin.loggedin",
            "admin.loggedin",
            "rabbitmq",
            new 
            { 
                adminId = admin.Id,
                username = admin.Username,
                email = admin.Email,
                timestamp = DateTime.UtcNow,
                eventType = "admin.loggedin",
                source = "auth-service"
            });

        // 2. Kafka Message - Successful login audit
        await OutboxHelper.AddToOutboxAsync(
            _outboxRepository,
            _unitOfWork,
            "admin.loggedin",
            "admin.loggedin",
            "kafka",
            new 
            { 
                eventType = "admin.loggedin",
                adminId = admin.Id,
                username = admin.Username,
                email = admin.Email,
                timestamp = DateTime.UtcNow,
                severity = "Low"
            });

        return AdminLoginResponse.CreateSuccess(token, admin.Id, admin.Username, admin.Email);
    }
}