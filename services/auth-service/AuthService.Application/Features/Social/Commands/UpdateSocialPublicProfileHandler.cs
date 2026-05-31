using MediatR;
using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AuthService.Application.Interfaces.Persistence;
using AuthService.Application.Common;

namespace AuthService.Application.Features.Social.Commands;

public sealed class UpdateSocialPublicProfileHandler : IRequestHandler<UpdateSocialPublicProfileCommand, SocialProfileCommandResponse>
{
    private readonly ISocialUserRepository _socialUserRepository;
    private readonly IOutboxRepository _outboxRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateSocialPublicProfileHandler(
        ISocialUserRepository socialUserRepository,
        IOutboxRepository outboxRepository,
        IUnitOfWork unitOfWork)
    {
        _socialUserRepository = socialUserRepository;
        _outboxRepository = outboxRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<SocialProfileCommandResponse> Handle(UpdateSocialPublicProfileCommand request, CancellationToken cancellationToken)
    {
        var user = await _socialUserRepository.GetByIdAsync(request.UserId);
        if (user == null)
        {
            return new SocialProfileCommandResponse(false, "Social user profile not found");
        }

        // Serialize the nested DTO into a camelCase JSON string matching our domain structure preference
        string? socialLinksJson = null;
        if (request.SocialLinks != null)
        {
            socialLinksJson = JsonSerializer.Serialize(request.SocialLinks, new JsonSerializerOptions 
            { 
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
            });
        }

        // Mutate the local entity state variables inside safe domain memory boundaries
        user.UpdatePublicProfile(
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

        _socialUserRepository.Update(user);

        // 💾 Claim Check Rule: Pack lightweight metadata parameters (~200 bytes)
        var profileChangedEvent = new
        {
            eventType = "social.user.public_profile_updated",
            userId = user.Id,
            email = user.Email,
            displayName = user.DisplayName,
            timestamp = DateTime.UtcNow
        };

        // 📬 Post Office Layer: Broadcast real-time notifications to update cache blocks or down-stream dependencies
        await OutboxHelper.AddToOutboxAsync(_outboxRepository, "social.user.profile_updated", "user.profile_updated", "rabbitmq", profileChangedEvent);

        // 🗄️ Big Archive Layer: Log chronological timeline entry to Aiven cluster disk permanently
        await OutboxHelper.AddToOutboxAsync(_outboxRepository, "auth-events", "social.user.profile_updated", "kafka", profileChangedEvent);

        // 🏗️ Rules Applied: SINGLE UNIFIED ATOMIC DATABASE TRANSACTION COMMIT BOUNDARY
        // This ensures your database changes and outbox event logs succeed or fail together.
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new SocialProfileCommandResponse(true, "Public profile updated successfully", new
        {
            user.FullName,
            user.JobTitle,
            user.Headline,
            user.Tagline,
            user.Bio,
            user.Phone,
            user.Location,
            user.Website,
            SocialLinks = string.IsNullOrEmpty(user.SocialLinks) 
                ? null 
                : JsonSerializer.Deserialize<object>(user.SocialLinks)
        });
    }
}
