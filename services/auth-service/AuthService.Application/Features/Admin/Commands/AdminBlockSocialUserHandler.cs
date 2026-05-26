using MediatR;
using AuthService.Application.Interfaces.Persistence;
using AuthService.Application.Common;

namespace AuthService.Application.Features.Admin.Commands;

public class AdminBlockSocialUserHandler : IRequestHandler<AdminBlockSocialUserCommand, bool>
{
    private readonly ISocialUserRepository _socialUserRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOutboxRepository _outboxRepository;

    public AdminBlockSocialUserHandler(
        ISocialUserRepository socialUserRepository, 
        IUnitOfWork unitOfWork,
        IOutboxRepository outboxRepository)
    {
        _socialUserRepository = socialUserRepository;
        _unitOfWork = unitOfWork;
        _outboxRepository = outboxRepository;
    }

    public async Task<bool> Handle(AdminBlockSocialUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _socialUserRepository.GetByIdAsync(request.UserId);
        
        if (user == null)
        {
            return false;
        }

        user.AdminBlock(request.Reason);
        _socialUserRepository.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Log user blocked event to Kafka
        await OutboxHelper.AddToOutboxAsync(
            _outboxRepository,
            _unitOfWork,
            "user.blocked",
            "user.blocked",
            "kafka",
            new
            {
                eventType = "user.blocked",
                userId = user.Id,
                email = user.Email,
                displayName = user.DisplayName,
                provider = user.Provider.ToString(),
                reason = request.Reason,
                timestamp = DateTime.UtcNow,
                severity = "Medium"
            });

        return true;
    }
}