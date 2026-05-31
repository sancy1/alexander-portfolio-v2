// File: AuthService.Application/Features/Admin/Commands/UpdateAdminProfileHandler.cs
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AuthService.Application.DTOs.Responses;
using AuthService.Application.Interfaces.Persistence;
using AuthService.Application.Common;

namespace AuthService.Application.Features.Admin.Commands;

public sealed class UpdateAdminProfileHandler : IRequestHandler<UpdateAdminProfileCommand, UpdateAdminProfileResponse>
{
    private readonly IAdminRepository _adminRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOutboxRepository _outboxRepository;

    public UpdateAdminProfileHandler(
        IAdminRepository adminRepository, 
        IUnitOfWork unitOfWork,
        IOutboxRepository outboxRepository)
    {
        _adminRepository = adminRepository;
        _unitOfWork = unitOfWork;
        _outboxRepository = outboxRepository;
    }

    public async Task<UpdateAdminProfileResponse> Handle(UpdateAdminProfileCommand request, CancellationToken cancellationToken)
    {
        var admin = await _adminRepository.GetByIdAsync(request.AdminId);
        
        if (admin == null)
        {
            return new UpdateAdminProfileResponse { Success = false, Message = "Admin not found" };
        }

        var oldUsername = admin.Username;
        var oldEmail = admin.Email;
        var changes = new List<string>();

        // Check if username is taken (if changing)
        if (!string.IsNullOrEmpty(request.Username) && request.Username != admin.Username)
        {
            var existing = await _adminRepository.GetByUsernameAsync(request.Username);
            if (existing != null)
            {
                return new UpdateAdminProfileResponse { Success = false, Message = "Username already taken" };
            }
            changes.Add($"username: {oldUsername} → {request.Username}");
        }

        // Check if email is taken (if changing)
        if (!string.IsNullOrEmpty(request.Email) && request.Email.ToLower() != admin.Email)
        {
            var existing = await _adminRepository.GetByEmailAsync(request.Email);
            if (existing != null)
            {
                return new UpdateAdminProfileResponse { Success = false, Message = "Email already registered" };
            }
            changes.Add($"email: {oldEmail} → {request.Email}");
        }

        // 1. Mutate tracking status parameters strictly inside in-memory state lines
        admin.UpdateProfile(request.Username, request.Email?.ToLower());
        _adminRepository.Update(admin);

        // 2. Queue outbox message entities inside the same tracking frame ONLY if data changes occurred
        if (changes.Any())
        {
            // 💾 Claim Check Rule: Keep payload under ~200 bytes containing primitive delta changes
            var profileUpdateEvent = new
            {
                eventType = "admin.profile_updated",
                adminId = admin.Id,
                username = admin.Username,
                email = admin.Email,
                changes = changes,
                timestamp = DateTime.UtcNow,
                severity = "Low"
            };

            // 📬 Post Office: Real-time broadcast notification outbox sync
            await OutboxHelper.AddToOutboxAsync(
                _outboxRepository,
                "admin.profile_updated",
                "admin.profile_updated",
                "rabbitmq",
                profileUpdateEvent);

            // 🗄️ Big Archive: Log chronological application history permanently to disk
            await OutboxHelper.AddToOutboxAsync(
                _outboxRepository,
                "auth-events", // Target active consumer routing streams directly
                "admin.profile_updated",
                "kafka",
                profileUpdateEvent);
        }

        // 🏗️ Rules Applied: SINGLE ATOMIC COMMIT FOR ALL PHASES
        // Admin updates and both outbox lines commit in one singular PostgreSQL transaction block.
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new UpdateAdminProfileResponse
        {
            Success = true,
            Username = admin.Username,
            Email = admin.Email
        };
    }
}
