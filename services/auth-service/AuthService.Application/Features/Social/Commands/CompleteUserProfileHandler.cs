// File: AuthService.Application/Features/Social/Commands/CompleteUserProfileHandler.cs
// Purpose: Handles social user profile completion
// Layer: Application

using MediatR;
using AuthService.Application.DTOs.Responses;
using AuthService.Application.Interfaces.Persistence;
using AuthService.Domain.Interfaces;

namespace AuthService.Application.Features.Social.Commands;

public class CompleteUserProfileHandler : IRequestHandler<CompleteUserProfileCommand, AuthResponse>
{
    private readonly ISocialUserRepository _socialUserRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IJwtGenerator _jwtGenerator;

    public CompleteUserProfileHandler(
        ISocialUserRepository socialUserRepository,
        IUnitOfWork unitOfWork,
        IJwtGenerator jwtGenerator)
    {
        _socialUserRepository = socialUserRepository;
        _unitOfWork = unitOfWork;
        _jwtGenerator = jwtGenerator;
    }

    public async Task<AuthResponse> Handle(CompleteUserProfileCommand request, CancellationToken cancellationToken)
    {
        var user = await _socialUserRepository.GetByIdAsync(request.UserId);
        
        if (user == null)
        {
            return AuthResponse.CreateFailure("User not found");
        }

        if (user.IsProfileComplete)
        {
            return AuthResponse.CreateFailure("Profile already completed");
        }

        // Complete the profile
        user.CompleteProfile(request.DisplayName, request.AvatarUrl);
        user.RecordLogin();
        
        _socialUserRepository.Update(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Generate JWT token
        var token = _jwtGenerator.GenerateUserToken(user);

        return AuthResponse.CreateSuccess(token, user.Id);
    }
}