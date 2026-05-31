using MediatR;
using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AuthService.Application.Interfaces.Persistence;
using AuthService.Application.Common;

namespace AuthService.Application.Features.Admin.Commands;

public sealed class UpdateAdminPublicProfileHandler : IRequestHandler<UpdateAdminPublicProfileCommand, PublicProfileCommandResponse>
{
    private readonly IAdminRepository _adminRepository;
    private readonly IOutboxRepository _outboxRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateAdminPublicProfileHandler(
        IAdminRepository adminRepository,
        IOutboxRepository outboxRepository,
        IUnitOfWork unitOfWork)
    {
        _adminRepository = adminRepository;
        _outboxRepository = outboxRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<PublicProfileCommandResponse> Handle(UpdateAdminPublicProfileCommand request, CancellationToken cancellationToken)
    {
        var admin = await _adminRepository.GetByIdAsync(request.AdminId);
        if (admin == null)
        {
            return new PublicProfileCommandResponse(false, "Admin profile entity not found");
        }

        // Serialize the nested DTO into a single JSON string matching our domain structure preference
        string? socialLinksJson = null;
        if (request.SocialLinks != null)
        {
            socialLinksJson = JsonSerializer.Serialize(request.SocialLinks, new JsonSerializerOptions 
            { 
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
            });
        }

        // Update the state properties via the safe encapsulated aggregate behavior method
        admin.UpdatePublicProfile(
            request.FullName,
            request.JobTitle,
            request.Headline,
            request.Tagline,
            request.Bio,
            request.Phone,
            request.Location,
            request.Website,
            socialLinksJson
        );

        _adminRepository.Update(admin);

        // 💾 Claim Check Rule: Keep event metadata records down to ~200 bytes primitives
        var profileChangedEvent = new
        {
            eventType = "admin.public_profile_updated",
            adminId = admin.Id,
            username = admin.Username,
            jobTitle = admin.JobTitle, // Shared primitives used by Go BlogService
            timestamp = DateTime.UtcNow
        };

        // 📬 Post Office: Broadcast instant lightweight session sync updates to surrounding contexts
        await OutboxHelper.AddToOutboxAsync(_outboxRepository, "admin.profile_updated", "admin.profile_updated", "rabbitmq", profileChangedEvent);

        // 🗄️ Big Archive: Log chronological timeline entry to Aiven cluster disk permanently
        await OutboxHelper.AddToOutboxAsync(_outboxRepository, "auth-events", "admin.profile_updated", "kafka", profileChangedEvent);

        // 🏗️ Rules Applied: SINGLE UNIFIED ATOMIC DATABASE TRANSACTION COMMIT BOUNDARY
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new PublicProfileCommandResponse(true, "Public profile updated successfully", new
        {
            admin.FullName,
            admin.JobTitle,
            admin.Headline,
            admin.Tagline,
            admin.Bio,
            admin.Phone,
            admin.Location,
            admin.Website,
            SocialLinks = string.IsNullOrEmpty(admin.SocialLinks) 
                ? null 
                : JsonSerializer.Deserialize<object>(admin.SocialLinks)
        });
    }
}
