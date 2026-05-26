// // File: AuthService.Application/Features/Social/Commands/HandleOAuthLoginHandler.cs
// // Purpose: Handles OAuth login with duplicate email prevention
// // Layer: Application

// using MediatR;
// using AuthService.Application.DTOs.Responses;
// using AuthService.Application.Interfaces.Persistence;
// using AuthService.Domain.Entities;
// using AuthService.Domain.Enums;
// using AuthService.Domain.Interfaces;

// namespace AuthService.Application.Features.Social.Commands;

// public class HandleOAuthLoginHandler : IRequestHandler<HandleOAuthLoginCommand, AuthResponse>
// {
//     private readonly ISocialUserRepository _socialUserRepository;
//     private readonly IUnitOfWork _unitOfWork;
//     private readonly IJwtGenerator _jwtGenerator;

//     public HandleOAuthLoginHandler(
//         ISocialUserRepository socialUserRepository,
//         IUnitOfWork unitOfWork,
//         IJwtGenerator jwtGenerator)
//     {
//         _socialUserRepository = socialUserRepository;
//         _unitOfWork = unitOfWork;
//         _jwtGenerator = jwtGenerator;
//     }

//     public async Task<AuthResponse> Handle(HandleOAuthLoginCommand request, CancellationToken cancellationToken)
//     {
//         // FIRST: Check if user exists by email (prevents duplicate emails across providers)
//         var existingByEmail = await _socialUserRepository.GetByEmailAsync(request.Email);
        
//         if (existingByEmail != null)
//         {
//             // User exists with this email - check if they are trying to login with same provider
//             if (existingByEmail.Provider == request.Provider && existingByEmail.ProviderId == request.ProviderId)
//             {
//                 // Same user logging in again
//                 if (existingByEmail.IsProfileComplete)
//                 {
//                     existingByEmail.RecordLogin();
//                     _socialUserRepository.Update(existingByEmail);
//                     await _unitOfWork.SaveChangesAsync(cancellationToken);
                    
//                     var token = _jwtGenerator.GenerateUserToken(existingByEmail);
//                     return AuthResponse.CreateSuccess(token, existingByEmail.Id);
//                 }
//                 return AuthResponse.CreateProfileIncomplete(existingByEmail.Id);
//             }
            
//             // Email already exists with DIFFERENT provider
//             // Example: User signed up with Google, now trying GitHub with same email
//             return AuthResponse.CreateFailure($"An account with email {request.Email} already exists. Please login using {existingByEmail.Provider} instead.");
//         }

//         // SECOND: Check if user exists by provider ID (for returning users)
//         var existingByProvider = await _socialUserRepository.GetByProviderIdAsync(request.ProviderId, request.Provider);

//         if (existingByProvider != null)
//         {
//             if (existingByProvider.IsProfileComplete)
//             {
//                 existingByProvider.RecordLogin();
//                 _socialUserRepository.Update(existingByProvider);
//                 await _unitOfWork.SaveChangesAsync(cancellationToken);
                
//                 var token = _jwtGenerator.GenerateUserToken(existingByProvider);
//                 return AuthResponse.CreateSuccess(token, existingByProvider.Id);
//             }
            
//             return AuthResponse.CreateProfileIncomplete(existingByProvider.Id);
//         }

//         // THIRD: New user - create partial record
//         var newUser = new SocialUser(
//             request.ProviderId,
//             request.Provider,
//             request.Email,
//             request.DisplayName
//         );

//         await _socialUserRepository.AddAsync(newUser);
//         await _unitOfWork.SaveChangesAsync(cancellationToken);

//         return AuthResponse.CreateProfileIncomplete(newUser.Id);
//     }
// }






























// File: AuthService.Application/Features/Social/Commands/HandleOAuthLoginHandler.cs
// Purpose: Handles OAuth login with duplicate email prevention and avatar capture
// Layer: Application

using MediatR;
using AuthService.Application.DTOs.Responses;
using AuthService.Application.Interfaces.Persistence;
using AuthService.Domain.Entities;
using AuthService.Domain.Enums;
using AuthService.Domain.Interfaces;

namespace AuthService.Application.Features.Social.Commands;

public class HandleOAuthLoginHandler : IRequestHandler<HandleOAuthLoginCommand, AuthResponse>
{
    private readonly ISocialUserRepository _socialUserRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IJwtGenerator _jwtGenerator;

    public HandleOAuthLoginHandler(
        ISocialUserRepository socialUserRepository,
        IUnitOfWork unitOfWork,
        IJwtGenerator jwtGenerator)
    {
        _socialUserRepository = socialUserRepository;
        _unitOfWork = unitOfWork;
        _jwtGenerator = jwtGenerator;
    }

    public async Task<AuthResponse> Handle(HandleOAuthLoginCommand request, CancellationToken cancellationToken)
    {
        // FIRST: Check if user exists by email (prevents duplicate emails across providers)
        var existingByEmail = await _socialUserRepository.GetByEmailAsync(request.Email);
        
        if (existingByEmail != null)
        {
            // User exists with this email - check if they are trying to login with same provider
            if (existingByEmail.Provider == request.Provider && existingByEmail.ProviderId == request.ProviderId)
            {
                // Update avatar if missing (in case user added avatar on provider side)
                if (string.IsNullOrEmpty(existingByEmail.AvatarUrl) && !string.IsNullOrEmpty(request.AvatarUrl))
                {
                    existingByEmail.UpdateAvatar(request.AvatarUrl);
                    _socialUserRepository.Update(existingByEmail);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                }
                
                // Same user logging in again
                if (existingByEmail.IsProfileComplete)
                {
                    existingByEmail.RecordLogin();
                    _socialUserRepository.Update(existingByEmail);
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                    
                    var token = _jwtGenerator.GenerateUserToken(existingByEmail);
                    return AuthResponse.CreateSuccess(token, existingByEmail.Id);
                }
                return AuthResponse.CreateProfileIncomplete(existingByEmail.Id);
            }
            
            // Email already exists with DIFFERENT provider
            return AuthResponse.CreateFailure($"An account with email {request.Email} already exists. Please login using {existingByEmail.Provider} instead.");
        }

        // SECOND: Check if user exists by provider ID (for returning users)
        var existingByProvider = await _socialUserRepository.GetByProviderIdAsync(request.ProviderId, request.Provider);

        if (existingByProvider != null)
        {
            // Update avatar if missing (in case user added avatar on provider side)
            if (string.IsNullOrEmpty(existingByProvider.AvatarUrl) && !string.IsNullOrEmpty(request.AvatarUrl))
            {
                existingByProvider.UpdateAvatar(request.AvatarUrl);
                _socialUserRepository.Update(existingByProvider);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
            
            if (existingByProvider.IsProfileComplete)
            {
                existingByProvider.RecordLogin();
                _socialUserRepository.Update(existingByProvider);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                
                var token = _jwtGenerator.GenerateUserToken(existingByProvider);
                return AuthResponse.CreateSuccess(token, existingByProvider.Id);
            }
            
            return AuthResponse.CreateProfileIncomplete(existingByProvider.Id);
        }

        // THIRD: New user - create partial record with avatar from OAuth provider
        var newUser = new SocialUser(
            request.ProviderId,
            request.Provider,
            request.Email,
            request.DisplayName
        );

        // Set avatar if provided by OAuth provider
        if (!string.IsNullOrEmpty(request.AvatarUrl))
        {
            newUser.UpdateAvatar(request.AvatarUrl);
        }

        await _socialUserRepository.AddAsync(newUser);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return AuthResponse.CreateProfileIncomplete(newUser.Id);
    }
}