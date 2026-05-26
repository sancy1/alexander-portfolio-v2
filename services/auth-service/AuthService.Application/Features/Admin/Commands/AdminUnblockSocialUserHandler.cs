using MediatR;
using AuthService.Application.Interfaces.Persistence;
using AuthService.Application.Common;

namespace AuthService.Application.Features.Admin.Commands;

public class AdminUnblockSocialUserHandler : IRequestHandler<AdminUnblockSocialUserCommand, bool>
{
    private readonly ISocialUserRepository _socialUserRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOutboxRepository _outboxRepository;

    public AdminUnblockSocialUserHandler(
        ISocialUserRepository socialUserRepository, 
        IUnitOfWork unitOfWork,
        IOutboxRepository outboxRepository)
    {
        _socialUserRepository = socialUserRepository;
        _unitOfWork = unitOfWork;
        _outboxRepository = outboxRepository;
    }

    public async Task<bool> Handle(AdminUnblockSocialUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _socialUserRepository.GetByIdAsync(request.UserId);
        
        if (user == null)
        {
            return false;
        }

        user.AdminUnblock();
        _socialUserRepository.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Log user unblocked event to Kafka via Outbox
        await OutboxHelper.AddToOutboxAsync(
            _outboxRepository,
            _unitOfWork,
            "user.unblocked",
            "user.unblocked",
            "kafka",
            new
            {
                eventType = "user.unblocked",
                userId = user.Id,
                email = user.Email,
                displayName = user.DisplayName,
                provider = user.Provider.ToString(),
                timestamp = DateTime.UtcNow,
                severity = "Low"
            });

        return true;
    }
}