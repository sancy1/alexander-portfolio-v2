using MediatR;
using AuthService.Application.DTOs.Responses;
using AuthService.Application.Interfaces.Persistence;
using AuthService.Application.Common;

namespace AuthService.Application.Features.Admin.Commands;

public class UpdateAdminProfileHandler : IRequestHandler<UpdateAdminProfileCommand, UpdateAdminProfileResponse>
{
    private readonly IAdminRepository _adminRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOutboxRepository _outboxRepository;

    public UpdateAdminProfileHandler(
        IAdminRepository adminRepository, 
        IUnitOfWork unitOfWork,
        IOutboxRepository outboxRepository)
    {
        _adminRepository = adminRepository;
        _unitOfWork = unitOfWork;
        _outboxRepository = outboxRepository;
    }

    public async Task<UpdateAdminProfileResponse> Handle(UpdateAdminProfileCommand request, CancellationToken cancellationToken)
    {
        var admin = await _adminRepository.GetByIdAsync(request.AdminId);
        
        if (admin == null)
        {
            return new UpdateAdminProfileResponse { Success = false, Message = "Admin not found" };
        }

        var oldUsername = admin.Username;
        var oldEmail = admin.Email;
        var changes = new List<string>();

        // Check if username is taken (if changing)
        if (!string.IsNullOrEmpty(request.Username) && request.Username != admin.Username)
        {
            var existing = await _adminRepository.GetByUsernameAsync(request.Username);
            if (existing != null)
            {
                return new UpdateAdminProfileResponse { Success = false, Message = "Username already taken" };
            }
            changes.Add($"username: {oldUsername} → {request.Username}");
        }

        // Check if email is taken (if changing)
        if (!string.IsNullOrEmpty(request.Email) && request.Email.ToLower() != admin.Email)
        {
            var existing = await _adminRepository.GetByEmailAsync(request.Email);
            if (existing != null)
            {
                return new UpdateAdminProfileResponse { Success = false, Message = "Email already registered" };
            }
            changes.Add($"email: {oldEmail} → {request.Email}");
        }

        admin.UpdateProfile(request.Username, request.Email?.ToLower());
        _adminRepository.Update(admin);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Log profile update if anything changed
        if (changes.Any())
        {
            await OutboxHelper.AddToOutboxAsync(
                _outboxRepository,
                _unitOfWork,
                "admin.profile_updated",
                "admin.profile_updated",
                "kafka",
                new
                {
                    eventType = "admin.profile_updated",
                    adminId = admin.Id,
                    username = admin.Username,
                    email = admin.Email,
                    changes = changes,
                    timestamp = DateTime.UtcNow,
                    severity = "Low"
                });
        }

        return new UpdateAdminProfileResponse
        {
            Success = true,
            Username = admin.Username,
            Email = admin.Email
        };
    }
}