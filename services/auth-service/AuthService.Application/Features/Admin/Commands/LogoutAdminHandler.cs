using MediatR;
using System.IdentityModel.Tokens.Jwt;
using AuthService.Application.Interfaces.Security;
using AuthService.Application.Interfaces.Persistence;
using AuthService.Application.Common;

namespace AuthService.Application.Features.Admin.Commands;

public class LogoutAdminHandler : IRequestHandler<LogoutAdminCommand, bool>
{
    private readonly ITokenBlacklistService _tokenBlacklistService;
    private readonly IOutboxRepository _outboxRepository;
    private readonly IUnitOfWork _unitOfWork;

    public LogoutAdminHandler(
        ITokenBlacklistService tokenBlacklistService,
        IOutboxRepository outboxRepository,
        IUnitOfWork unitOfWork)
    {
        _tokenBlacklistService = tokenBlacklistService;
        _outboxRepository = outboxRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(LogoutAdminCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var jwtToken = new JwtSecurityTokenHandler().ReadJwtToken(request.Token);
            var adminIdClaim = jwtToken.Claims
                .FirstOrDefault(c => c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")
                ?.Value;

            // Blacklist the token — expiry is handled internally by TokenBlacklistService (30 days)
            await _tokenBlacklistService.BlacklistTokenAsync(request.Token);

            if (!string.IsNullOrEmpty(adminIdClaim))
            {
                await OutboxHelper.AddToOutboxAsync(
                    _outboxRepository,
                    _unitOfWork,
                    "admin.loggedout",
                    "admin.loggedout",
                    "kafka",
                    new
                    {
                        eventType = "admin.loggedout",
                        adminId = Guid.Parse(adminIdClaim),
                        timestamp = DateTime.UtcNow,
                        severity = "Low"
                    });
            }

            return true;
        }
        catch
        {
            // Token invalid or already expired — client discards it anyway
            return true;
        }
    }
}