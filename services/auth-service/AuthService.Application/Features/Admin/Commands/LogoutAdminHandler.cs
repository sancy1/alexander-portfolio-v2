// File: AuthService.Application/Features/Admin/Commands/LogoutAdminHandler.cs
using MediatR;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AuthService.Application.Interfaces.Security;
using AuthService.Application.Interfaces.Persistence;
using AuthService.Application.Common;

namespace AuthService.Application.Features.Admin.Commands;

public sealed class LogoutAdminHandler : IRequestHandler<LogoutAdminCommand, bool>
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
        string? adminIdClaim = null;

        try
        {
            // 💡 Extract parameters before initiating state mutations
            var jwtToken = new JwtSecurityTokenHandler().ReadJwtToken(request.Token);
            adminIdClaim = jwtToken.Claims
                .FirstOrDefault(c => c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier" || c.Type == "sub")
                ?.Value;
        }
        catch
        {
            // Token is malformed or drastically invalid - client discards it safely anyway
            return true;
        }

        try
        {
            // 🧠 Memory Shield Rule: Blacklist the token in Redis immediately to stop replay attacks
            await _tokenBlacklistService.BlacklistTokenAsync(request.Token);

            if (!string.IsNullOrEmpty(adminIdClaim) && Guid.TryParse(adminIdClaim, out var parsedAdminId))
            {
                // 💾 Claim Check Rule: Keep event payloads down to ~200 bytes primitive types
                var logoutEvent = new
                {
                    eventType = "admin.loggedout",
                    adminId = parsedAdminId,
                    timestamp = DateTime.UtcNow,
                    severity = "Low"
                };

                // 📬 Post Office Workflow: Real-time ephemeral broadcast notifications
                await OutboxHelper.AddToOutboxAsync(
                    _outboxRepository,
                    "admin.loggedout",
                    "admin.loggedout",
                    "rabbitmq",
                    logoutEvent);

                // 🗄️ Big Archive Log Workflow: Sequential long-term tracking
                await OutboxHelper.AddToOutboxAsync(
                    _outboxRepository,
                    "auth-events", // Aligned directly with KafkaConsumer subscriptions
                    "admin.loggedout",
                    "kafka",
                    logoutEvent);

                // 🏗️ Atomic Integrity: Ensures outbox logs commit cleanly to Postgres
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            return true;
        }
        catch (Exception)
        {
            // Propagate critical infrastructure failures (e.g. Database Down) 
            // so controllers can return a 500 error instead of false security tokens
            throw;
        }
    }
}
