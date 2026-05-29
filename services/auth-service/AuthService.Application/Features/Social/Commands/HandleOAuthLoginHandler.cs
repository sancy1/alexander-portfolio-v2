// File: services/auth-service/AuthService.Application/Features/Social/Commands/HandleOAuthLoginHandler.cs
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;
using AuthService.Application.DTOs.Responses;
using AuthService.Application.DTOs.Events; // Enforces our clean cross-service event schemas
using AuthService.Application.Interfaces.Persistence;
using AuthService.Application.Common;
using AuthService.Domain.Entities;
using AuthService.Domain.Enums;
using AuthService.Domain.Interfaces;

namespace AuthService.Application.Features.Social.Commands;

public class HandleOAuthLoginHandler : IRequestHandler<HandleOAuthLoginCommand, AuthResponse>
{
    private readonly ISocialUserRepository _socialUserRepository;
    private readonly IOutboxRepository _outboxRepository; // 👇 Injected to record outbox table state updates
    private readonly IUnitOfWork _unitOfWork;
    private readonly IJwtGenerator _jwtGenerator;

    public HandleOAuthLoginHandler(
        ISocialUserRepository socialUserRepository,
        IOutboxRepository outboxRepository,
        IUnitOfWork unitOfWork,
        IJwtGenerator jwtGenerator)
    {
        _socialUserRepository = socialUserRepository;
        _outboxRepository = outboxRepository;
        _unitOfWork = unitOfWork;
        _jwtGenerator = jwtGenerator;
    }

    public async Task<AuthResponse> Handle(HandleOAuthLoginCommand request, CancellationToken cancellationToken)
    {
        // Enforce transaction isolation boundary for PostgreSQL row mutations
        using var transaction = await _unitOfWork.BeginTransactionAsync();
        try
        {
            // ─── FIRST STAGE: DUPLICATE EMAIL PREVENTION VALIDATION ───
            var existingByEmail = await _socialUserRepository.GetByEmailAsync(request.Email);
            
            if (existingByEmail != null)
            {
                if (existingByEmail.Provider == request.Provider && existingByEmail.ProviderId == request.ProviderId)
                {
                    // Check and execute avatar patches in memory 
                    if (string.IsNullOrEmpty(existingByEmail.AvatarUrl) && !string.IsNullOrEmpty(request.AvatarUrl))
                    {
                        existingByEmail.UpdateAvatar(request.AvatarUrl);
                    }
                    
                    if (existingByEmail.IsProfileComplete)
                    {
                        existingByEmail.RecordLogin();
                        _socialUserRepository.Update(existingByEmail);
                        
                        // Stage single consolidated outbox message entry routed to BOTH brokers ("both")
                        await OutboxHelper.AddToOutboxAsync(_outboxRepository, "social.user.loggedin", "user.loggedin", "both", new UserLoggedInEvent
                        {
                            EventType = "social.user.loggedin",
                            OccurredAt = DateTime.UtcNow,
                            UserId = existingByEmail.Id,
                            Email = existingByEmail.Email,
                            UserType = "SocialUser",
                            LoginMethod = request.Provider.ToString().ToLower(),
                            ClientIp = request.ClientIp,
                            UserAgent = request.UserAgent
                        });

                        await _unitOfWork.SaveChangesAsync(cancellationToken);
                        await transaction.CommitAsync(cancellationToken);
                        
                        var token = _jwtGenerator.GenerateUserToken(existingByEmail);
                        return AuthResponse.CreateSuccess(token, existingByEmail.Id);
                    }

                    // User exists but registration is half-baked
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);
                    return AuthResponse.CreateProfileIncomplete(existingByEmail.Id);
                }
                
                // Email account mismatch collision
                await OutboxHelper.AddToOutboxAsync(_outboxRepository, "security.failed_oauth_login", "security.failed_login", "kafka", new
                {
                    eventType = "security.failed_oauth_login",
                    email = request.Email,
                    occurredAt = DateTime.UtcNow,
                    reason = "provider_mismatch",
                    attemptedProvider = request.Provider.ToString(),
                    existingProvider = existingByEmail.Provider.ToString(),
                    clientIp = request.ClientIp,
                    userAgent = request.UserAgent,
                    severity = "Medium"
                });
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                
                return AuthResponse.CreateFailure($"An account with email {request.Email} already exists. Please login using {existingByEmail.Provider} instead.");
            }

            // ─── SECOND STAGE: PROVIDER CORRELATION MATCHES ───
            var existingByProvider = await _socialUserRepository.GetByProviderIdAsync(request.ProviderId, request.Provider);

            if (existingByProvider != null)
            {
                if (string.IsNullOrEmpty(existingByProvider.AvatarUrl) && !string.IsNullOrEmpty(request.AvatarUrl))
                {
                    existingByProvider.UpdateAvatar(request.AvatarUrl);
                }
                
                if (existingByProvider.IsProfileComplete)
                {
                    existingByProvider.RecordLogin();
                    _socialUserRepository.Update(existingByProvider);
                    
                    await OutboxHelper.AddToOutboxAsync(_outboxRepository, "social.user.loggedin", "user.loggedin", "both", new UserLoggedInEvent
                    {
                        EventType = "social.user.loggedin",
                        OccurredAt = DateTime.UtcNow,
                        UserId = existingByProvider.Id,
                        Email = existingByProvider.Email,
                        UserType = "SocialUser",
                        LoginMethod = request.Provider.ToString().ToLower(),
                        ClientIp = request.ClientIp,
                        UserAgent = request.UserAgent
                    });

                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                    await transaction.CommitAsync(cancellationToken);
                    
                    var token = _jwtGenerator.GenerateUserToken(existingByProvider);
                    return AuthResponse.CreateSuccess(token, existingByProvider.Id);
                }
                
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return AuthResponse.CreateProfileIncomplete(existingByProvider.Id);
            }

            // ─── THIRD STAGE: COLD SIGNUP REGISTRATION INTERCEPT ───
            var newUser = new SocialUser(request.ProviderId, request.Provider, request.Email, request.DisplayName);

            if (!string.IsNullOrEmpty(request.AvatarUrl))
            {
                newUser.UpdateAvatar(request.AvatarUrl);
            }

            await _socialUserRepository.AddAsync(newUser);
            
            // Log registration history event to Kafka for big data and analytical archiving patterns
            await OutboxHelper.AddToOutboxAsync(_outboxRepository, "social.user.registered", "user.registered", "kafka", new UserRegisteredEvent
            {
                EventType = "social.user.registered",
                OccurredAt = DateTime.UtcNow,
                UserId = newUser.Id,
                Email = newUser.Email,
                DisplayName = newUser.DisplayName,
                Provider = request.Provider.ToString().ToLower(),
                IsProfileComplete = false,
                ClientIp = request.ClientIp,
                UserAgent = request.UserAgent
            });

            // Flush everything safely in one network write call
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return AuthResponse.CreateProfileIncomplete(newUser.Id);
        }
        catch (Exception)
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
