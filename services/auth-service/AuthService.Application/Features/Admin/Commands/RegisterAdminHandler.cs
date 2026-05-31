// File: AuthService.Application/Features/Admin/Commands/RegisterAdminHandler.cs
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;
using AuthService.Application.DTOs.Responses;
using AuthService.Domain.Entities;
using AuthService.Domain.Enums;
using AuthService.Domain.Interfaces;
using AuthService.Application.Interfaces.Persistence;
using AuthService.Application.Interfaces.Security;
using AuthService.Application.Common;
using AdminEntity = AuthService.Domain.Entities.Admin;

namespace AuthService.Application.Features.Admin.Commands;

public sealed class RegisterAdminHandler : IRequestHandler<RegisterAdminCommand, AuthResponse>
{
    private readonly IAdminRepository _adminRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IAdminKeyValidator _adminKeyValidator;
    private readonly IJwtGenerator _jwtGenerator;
    private readonly IOutboxRepository _outboxRepository;

    public RegisterAdminHandler(
        IAdminRepository adminRepository,
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        IAdminKeyValidator adminKeyValidator,
        IJwtGenerator jwtGenerator,
        IOutboxRepository outboxRepository)
    {
        _adminRepository = adminRepository;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _adminKeyValidator = adminKeyValidator;
        _jwtGenerator = jwtGenerator;
        _outboxRepository = outboxRepository;
    }

    public async Task<AuthResponse> Handle(RegisterAdminCommand request, CancellationToken cancellationToken)
    {
        // 1. Guard Clauses & Access Validations
        if (!_adminKeyValidator.IsValidAdminKey(request.AdminKey))
        {
            return AuthResponse.CreateFailure("Invalid admin key provided");
        }

        var existingByUsername = await _adminRepository.GetByUsernameAsync(request.Username);
        if (existingByUsername != null)
        {
            return AuthResponse.CreateFailure("Username already taken");
        }

        var existingByEmail = await _adminRepository.GetByEmailAsync(request.Email);
        if (existingByEmail != null)
        {
            return AuthResponse.CreateFailure("Email already registered");
        }

        // 2. State Transformation and Cryptography
        var hashedPassword = _passwordHasher.HashPassword(request.Password);

        var adminEntity = new AdminEntity(
            request.Username,
            request.Email.ToLowerInvariant(),
            hashedPassword,
            UserRole.Admin
        );

        // 3. Track Intent Locally (In-Memory Unit of Work Queue)
        await _adminRepository.AddAsync(adminEntity);

        // 💾 Claim Check Rule: Pack lightweight metadata parameters (~200 bytes)
        var registrationEvent = new
        {
            eventType = "admin.registered",
            adminId = adminEntity.Id,
            username = adminEntity.Username,
            email = adminEntity.Email,
            role = adminEntity.Role.ToString(),
            timestamp = DateTime.UtcNow
        };

        // 📬 Post Office Channel: Microservice messaging notifications (Welcome emails)
        await OutboxHelper.AddToOutboxAsync(
            _outboxRepository,
            "admin.registered",
            "admin.registered",
            "rabbitmq",
            registrationEvent);

        // 🗄️ Big Archive Channel: Persistent event sourcing cluster timeline
        await OutboxHelper.AddToOutboxAsync(
            _outboxRepository,
            "auth-events", // Aligned perfectly with your active KafkaConsumer subscription topic
            "admin.registered",
            "kafka",
            registrationEvent);

        // 🏗️ Rules Applied: SINGLE ATOMIC TRANSACTION COMMIT
        // This guarantees that either everything saves successfully or the database completely rolls back.
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // 🧠 Post-Commit Security Manifest Generation
        var token = _jwtGenerator.GenerateAdminToken(adminEntity);

        return AuthResponse.CreateSuccess(token, adminEntity.Id);
    }
}
