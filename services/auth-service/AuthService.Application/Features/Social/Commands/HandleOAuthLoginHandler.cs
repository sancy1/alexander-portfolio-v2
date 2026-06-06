// // File: AuthService.Application/Features/Social/Commands/HandleOAuthLoginHandler.cs
// using MediatR;
// using System;
// using System.Threading;
// using System.Threading.Tasks;
// using AuthService.Application.DTOs.Responses;
// using AuthService.Application.DTOs.Events;
// using AuthService.Application.Interfaces.Persistence;
// using AuthService.Application.Common;
// using AuthService.Domain.Entities;
// using AuthService.Domain.Enums;
// using AuthService.Domain.Interfaces;

// namespace AuthService.Application.Features.Social.Commands;

// public sealed class HandleOAuthLoginHandler : IRequestHandler<HandleOAuthLoginCommand, AuthResponse>
// {
//     private readonly ISocialUserRepository _socialUserRepository;
//     private readonly IOutboxRepository _outboxRepository;
//     private readonly IUnitOfWork _unitOfWork;
//     private readonly IJwtGenerator _jwtGenerator;

//     public HandleOAuthLoginHandler(
//         ISocialUserRepository socialUserRepository,
//         IOutboxRepository outboxRepository,
//         IUnitOfWork unitOfWork,
//         IJwtGenerator jwtGenerator)
//     {
//         _socialUserRepository = socialUserRepository;
//         _outboxRepository = outboxRepository;
//         _unitOfWork = unitOfWork;
//         _jwtGenerator = jwtGenerator;
//     }

//     public async Task<AuthResponse> Handle(HandleOAuthLoginCommand request, CancellationToken cancellationToken)
//     {
//         // ─── FIRST STAGE: DUPLICATE EMAIL PREVENTION VALIDATION ───
//         var existingByEmail = await _socialUserRepository.GetByEmailAsync(request.Email);
        
//         if (existingByEmail != null)
//         {
//             if (existingByEmail.Provider == request.Provider && existingByEmail.ProviderId == request.ProviderId)
//             {
//                 if (string.IsNullOrEmpty(existingByEmail.AvatarUrl) && !string.IsNullOrEmpty(request.AvatarUrl))
//                 {
//                     existingByEmail.UpdateAvatar(request.AvatarUrl);
//                 }
                
//                 if (existingByEmail.IsProfileComplete)
//                 {
//                     existingByEmail.RecordLogin();
//                     _socialUserRepository.Update(existingByEmail);
                    
//                     var loginEvent = new UserLoggedInEvent
//                     {
//                         EventType = "social.user.loggedin",
//                         OccurredAt = DateTime.UtcNow,
//                         UserId = existingByEmail.Id,
//                         Email = existingByEmail.Email,
//                         UserType = "SocialUser",
//                         LoginMethod = request.Provider.ToString().ToLower(),
//                         ClientIp = request.ClientIp,
//                         UserAgent = request.UserAgent
//                     };

//                     // 📬 Post Office Channel: Microservice messaging notification tasks
//                     await OutboxHelper.AddToOutboxAsync(_outboxRepository, "social.user.loggedin", "user.loggedin", "rabbitmq", loginEvent);
                    
//                     // 🗄️ Big Archive Channel: Permanent chronological event history
//                     await OutboxHelper.AddToOutboxAsync(_outboxRepository, "auth-events", "social.user.loggedin", "kafka", loginEvent);

//                     // 🏗️ Single Atomic Push for the Success Pathway
//                     await _unitOfWork.SaveChangesAsync(cancellationToken);
                    
//                     var token = _jwtGenerator.GenerateUserToken(existingByEmail);
//                     return AuthResponse.CreateSuccess(token, existingByEmail.Id);
//                 }

//                 _socialUserRepository.Update(existingByEmail);
//                 // 🏗️ Single Atomic Push for the Profile Incomplete Pathway
//                 await _unitOfWork.SaveChangesAsync(cancellationToken);
//                 return AuthResponse.CreateProfileIncomplete(existingByEmail.Id);
//             }
            
//             // Email account mismatch collision - send to Kafka only (security audit trail)
//             var mismatchPayload = new
//             {
//                 eventType = "security.failed_oauth_login",
//                 email = request.Email,
//                 occurredAt = DateTime.UtcNow,
//                 reason = "provider_mismatch",
//                 attemptedProvider = request.Provider.ToString(),
//                 existingProvider = existingByEmail.Provider.ToString(),
//                 clientIp = request.ClientIp,
//                 userAgent = request.UserAgent,
//                 severity = "Medium"
//             };

//             await OutboxHelper.AddToOutboxAsync(
//                 _outboxRepository,
//                 "security-audit-logs",
//                 "security.failed_oauth_login",
//                 "kafka",
//                 mismatchPayload);
            
//             // 🏗️ Single Atomic Push for the Security Violation Pathway
//             await _unitOfWork.SaveChangesAsync(cancellationToken);
            
//             return AuthResponse.CreateFailure($"An account with email {request.Email} already exists. Please login using {existingByEmail.Provider} instead.");
//         }

//         // ─── SECOND STAGE: PROVIDER CORRELATION MATCHES ───
//         var existingByProvider = await _socialUserRepository.GetByProviderIdAsync(request.ProviderId, request.Provider);

//         if (existingByProvider != null)
//         {
//             if (string.IsNullOrEmpty(existingByProvider.AvatarUrl) && !string.IsNullOrEmpty(request.AvatarUrl))
//             {
//                 existingByProvider.UpdateAvatar(request.AvatarUrl);
//             }
            
//             if (existingByProvider.IsProfileComplete)
//             {
//                 existingByProvider.RecordLogin();
//                 _socialUserRepository.Update(existingByProvider);
                
//                 var loginEvent = new UserLoggedInEvent
//                 {
//                     EventType = "social.user.loggedin",
//                     OccurredAt = DateTime.UtcNow,
//                     UserId = existingByProvider.Id,
//                     Email = existingByProvider.Email,
//                     UserType = "SocialUser",
//                     LoginMethod = request.Provider.ToString().ToLower(),
//                     ClientIp = request.ClientIp,
//                     UserAgent = request.UserAgent
//                 };

//                 await OutboxHelper.AddToOutboxAsync(_outboxRepository, "social.user.loggedin", "user.loggedin", "rabbitmq", loginEvent);
//                 await OutboxHelper.AddToOutboxAsync(_outboxRepository, "auth-events", "social.user.loggedin", "kafka", loginEvent);

//                 // 🏗️ Single Atomic Push for the Success Pathway
//                 await _unitOfWork.SaveChangesAsync(cancellationToken);
                
//                 var token = _jwtGenerator.GenerateUserToken(existingByProvider);
//                 return AuthResponse.CreateSuccess(token, existingByProvider.Id);
//             }
            
//             _socialUserRepository.Update(existingByProvider);
//             // 🏗️ Single Atomic Push for the Provider Path Profile Incomplete Pathway
//             await _unitOfWork.SaveChangesAsync(cancellationToken);
//             return AuthResponse.CreateProfileIncomplete(existingByProvider.Id);
//         }

//         // ─── THIRD STAGE: COLD SIGNUP REGISTRATION INTERCEPT ───
//         var newUser = new SocialUser(request.ProviderId, request.Provider, request.Email, request.DisplayName);

//         if (!string.IsNullOrEmpty(request.AvatarUrl))
//         {
//             newUser.UpdateAvatar(request.AvatarUrl);
//         }

//         await _socialUserRepository.AddAsync(newUser);
        
//         var registrationEvent = new UserRegisteredEvent
//         {
//             EventType = "social.user.registered",
//             OccurredAt = DateTime.UtcNow,
//             UserId = newUser.Id,
//             Email = newUser.Email,
//             DisplayName = newUser.DisplayName,
//             Provider = request.Provider.ToString().ToLower(),
//             IsProfileComplete = false,
//             ClientIp = request.ClientIp,
//             UserAgent = request.UserAgent
//         };

//         await OutboxHelper.AddToOutboxAsync(_outboxRepository, "social.user.registered", "user.registered", "rabbitmq", registrationEvent);
//         await OutboxHelper.AddToOutboxAsync(_outboxRepository, "auth-events", "social.user.registered", "kafka", registrationEvent);

//         // 🏗️ Single Atomic Push for the Cold Registration Pathway
//         await _unitOfWork.SaveChangesAsync(cancellationToken);

//         return AuthResponse.CreateProfileIncomplete(newUser.Id);
//     }
// }



























// File: services/auth-service/AuthService.Application/Features/Social/Commands/HandleOAuthLoginHandler.cs

using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;
using AuthService.Application.DTOs.Responses;
using AuthService.Application.Interfaces.Persistence;
using AuthService.Application.Common;
using AuthService.Domain.Entities;
using AuthService.Domain.Enums;
using AuthService.Domain.Interfaces;

namespace AuthService.Application.Features.Social.Commands;

public sealed class HandleOAuthLoginHandler : IRequestHandler<HandleOAuthLoginCommand, AuthResponse>
{
    private readonly ISocialUserRepository _socialUserRepository;
    private readonly IOutboxRepository _outboxRepository;
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
        // ─── FIRST STAGE: DUPLICATE EMAIL PREVENTION VALIDATION ───
        var existingByEmail = await _socialUserRepository.GetByEmailAsync(request.Email.ToLowerInvariant());
        
        if (existingByEmail != null)
        {
            if (existingByEmail.Provider == request.Provider && existingByEmail.ProviderId == request.ProviderId)
            {
                // ====================================================================
                // SECURITY PATHWAY A: ACCOUNT IS DELETED / PENDING PURGE
                // ====================================================================
                if (existingByEmail.IsDeleted)
                {
                    var failedDeletedPayload = new
                    {
                        email = existingByEmail.Email, // Resolved: Primitive string access directly
                        reason = "account_soft_deleted",
                        severity = "Medium",
                        clientIp = request.ClientIp,
                        userAgent = request.UserAgent
                    };

                    await OutboxHelper.AddToOutboxAsync(
                        _outboxRepository,
                        eventType: "security.failed_oauth",
                        routingKey: "security.failed_oauth",
                        broker: "rabbitmq",
                        payload: failedDeletedPayload,
                        userId: existingByEmail.Id.ToString(),
                        userType: "social-user");

                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                    return AuthResponse.CreateFailure("This account is scheduled for deletion. Please cancel deletion first.");
                }

                // ====================================================================
                // SECURITY PATHWAY B: ACCOUNT SUSPENDED OR BLOCKED
                // ====================================================================
                if (existingByEmail.Status == AccountStatus.Suspended || existingByEmail.IsAdminBlocked)
                {
                    var failedBlockedPayload = new
                    {
                        email = existingByEmail.Email, // Resolved: Primitive string access directly
                        reason = existingByEmail.IsAdminBlocked ? "admin_blocked" : "suspended",
                        blockReason = existingByEmail.AdminBlockReason ?? "Violation of policy",
                        severity = "High",
                        clientIp = request.ClientIp,
                        userAgent = request.UserAgent
                    };

                    await OutboxHelper.AddToOutboxAsync(
                        _outboxRepository,
                        eventType: "security-audit-logs",
                        routingKey: "security.failed_oauth_login",
                        broker: "kafka",
                        payload: failedBlockedPayload,
                        userId: existingByEmail.Id.ToString(),
                        userType: "social-user");

                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                    return AuthResponse.CreateFailure("Your access has been suspended. Contact security operations.");
                }

                // Sync fresh avatar images if provided by OAuth provider
                if (string.IsNullOrEmpty(existingByEmail.AvatarUrl) && !string.IsNullOrEmpty(request.AvatarUrl))
                {
                    existingByEmail.UpdateAvatar(request.AvatarUrl);
                }
                
                if (existingByEmail.IsProfileComplete)
                {
                    existingByEmail.RecordLogin();
                    _socialUserRepository.Update(existingByEmail);
                    
                    var loginPayload = new
                    {
                        displayName = existingByEmail.DisplayName,
                        email = existingByEmail.Email, // Resolved: Safe primitive string mapping
                        avatarUrl = existingByEmail.AvatarUrl ?? string.Empty,
                        loginTime = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                        loginMethod = request.Provider.ToString().ToLowerInvariant(),
                        clientIp = request.ClientIp,
                        userAgent = request.UserAgent
                    };

                    // 📬 Post Office Channel: Ephemeral microservice event broadcasts
                    await OutboxHelper.AddToOutboxAsync(
                        _outboxRepository,
                        eventType: "social.user.loggedin",
                        routingKey: "social.user.loggedin",
                        broker: "rabbitmq",
                        payload: loginPayload,
                        userId: existingByEmail.Id.ToString(),
                        userType: "social-user");

                    // 🗄️ Big Archive Channel: Permanent compliance log stream
                    await OutboxHelper.AddToOutboxAsync(
                        _outboxRepository,
                        eventType: "auth-events",
                        routingKey: "social.user.loggedin",
                        broker: "kafka",
                        payload: loginPayload,
                        userId: existingByEmail.Id.ToString(),
                        userType: "social-user");

                    // Commit database entities and outbox rows in a single step
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                    
                    var token = _jwtGenerator.GenerateUserToken(existingByEmail);
                    return AuthResponse.CreateSuccess(token, existingByEmail.Id);
                }

                _socialUserRepository.Update(existingByEmail);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                return AuthResponse.CreateProfileIncomplete(existingByEmail.Id);
            }
            
            // Email account mismatch collision - track inside Kafka only (security audit trail)
            var mismatchPayload = new
            {
                email = request.Email,
                reason = "provider_mismatch",
                attemptedProvider = request.Provider.ToString().ToLowerInvariant(),
                existingProvider = existingByEmail.Provider.ToString().ToLowerInvariant(),
                clientIp = request.ClientIp,
                userAgent = request.UserAgent,
                severity = "Medium"
            };

            await OutboxHelper.AddToOutboxAsync(
                _outboxRepository,
                eventType: "security-audit-logs",
                routingKey: "security.failed_oauth_login",
                broker: "kafka",
                payload: mismatchPayload,
                userId: existingByEmail.Id.ToString(),
                userType: "social-user");
            
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            
            return AuthResponse.CreateFailure($"An account with email {request.Email} already exists. Please login using {existingByEmail.Provider} instead.");
        }

        // ─── SECOND STAGE: PROVIDER CORRELATION MATCHES ───
        var existingByProvider = await _socialUserRepository.GetByProviderIdAsync(request.ProviderId, request.Provider);

        if (existingByProvider != null)
        {
            // Security verification checkpoint for direct correlation pathways
            if (existingByProvider.IsDeleted)
            {
                return AuthResponse.CreateFailure("This account is scheduled for deletion. Please cancel deletion first.");
            }

            if (existingByProvider.Status == AccountStatus.Suspended || existingByProvider.IsAdminBlocked)
            {
                return AuthResponse.CreateFailure("Your access has been suspended. Contact security operations.");
            }

            if (string.IsNullOrEmpty(existingByProvider.AvatarUrl) && !string.IsNullOrEmpty(request.AvatarUrl))
            {
                existingByProvider.UpdateAvatar(request.AvatarUrl);
            }
            
            if (existingByProvider.IsProfileComplete)
            {
                existingByProvider.RecordLogin();
                _socialUserRepository.Update(existingByProvider);
                
                var loginPayload = new
                {
                    displayName = existingByProvider.DisplayName,
                    email = existingByProvider.Email, // Resolved: Primitive string access
                    avatarUrl = existingByProvider.AvatarUrl ?? string.Empty,
                    loginTime = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
                    loginMethod = request.Provider.ToString().ToLowerInvariant(),
                    clientIp = request.ClientIp,
                    userAgent = request.UserAgent
                };

                await OutboxHelper.AddToOutboxAsync(
                    _outboxRepository,
                    eventType: "social.user.loggedin",
                    routingKey: "social.user.loggedin",
                    broker: "rabbitmq",
                    payload: loginPayload,
                    userId: existingByProvider.Id.ToString(),
                    userType: "social-user");

                await OutboxHelper.AddToOutboxAsync(
                    _outboxRepository,
                    eventType: "auth-events",
                    routingKey: "social.user.loggedin",
                    broker: "kafka",
                    payload: loginPayload,
                    userId: existingByProvider.Id.ToString(),
                    userType: "social-user");

                await _unitOfWork.SaveChangesAsync(cancellationToken);
                
                var token = _jwtGenerator.GenerateUserToken(existingByProvider);
                return AuthResponse.CreateSuccess(token, existingByProvider.Id);
            }
            
            _socialUserRepository.Update(existingByProvider);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return AuthResponse.CreateProfileIncomplete(existingByProvider.Id);
        }

        // ─── THIRD STAGE: COLD SIGNUP REGISTRATION INTERCEPT ───
        var newUser = new SocialUser(request.ProviderId, request.Provider, request.Email, request.DisplayName);

        if (!string.IsNullOrEmpty(request.AvatarUrl))
        {
            newUser.UpdateAvatar(request.AvatarUrl);
        }

        await _socialUserRepository.AddAsync(newUser);
        
        var registrationPayload = new
        {
            displayName = newUser.DisplayName,
            email = newUser.Email, // Resolved: Stripped .Value object pathing safely
            provider = request.Provider.ToString().ToLowerInvariant(),
            registeredAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            isProfileComplete = false,
            clientIp = request.ClientIp,
            userAgent = request.UserAgent
        };

        await OutboxHelper.AddToOutboxAsync(
            _outboxRepository,
            eventType: "social.user.registered",
            routingKey: "social.user.registered",
            broker: "rabbitmq",
            payload: registrationPayload,
            userId: newUser.Id.ToString(),
            userType: "social-user");

        await OutboxHelper.AddToOutboxAsync(
            _outboxRepository,
            eventType: "auth-events",
            routingKey: "social.user.registered",
            broker: "kafka",
            payload: registrationPayload,
            userId: newUser.Id.ToString(),
            userType: "social-user");

        // Execute unified atomic context commit inside PostgreSQL
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var registerToken = _jwtGenerator.GenerateUserToken(newUser);
        return AuthResponse.CreateSuccess(registerToken, newUser.Id);
    }
}