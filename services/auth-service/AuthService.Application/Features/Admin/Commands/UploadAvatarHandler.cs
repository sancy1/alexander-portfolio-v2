using MediatR;
using AuthService.Application.Interfaces.Persistence;
using AuthService.Application.Interfaces.Services;

namespace AuthService.Application.Features.Admin.Commands;

public class UploadAvatarHandler : IRequestHandler<UploadAvatarCommand, string?>
{
    private readonly IAdminRepository _adminRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICloudinaryService _cloudinaryService;

    public UploadAvatarHandler(
        IAdminRepository adminRepository,
        IUnitOfWork unitOfWork,
        ICloudinaryService cloudinaryService)
    {
        _adminRepository = adminRepository;
        _unitOfWork = unitOfWork;
        _cloudinaryService = cloudinaryService;
    }

    public async Task<string?> Handle(UploadAvatarCommand request, CancellationToken cancellationToken)
    {
        var admin = await _adminRepository.GetByIdAsync(request.AdminId);
        
        if (admin == null)
        {
            return null;
        }

        if (request.Avatar == null || request.Avatar.Length == 0)
        {
            throw new ArgumentException("No avatar file provided");
        }

        var avatarUrl = await _cloudinaryService.UploadImageAsync(request.Avatar, "admin_avatars");
        
        admin.UpdateAvatar(avatarUrl);
        
        _adminRepository.Update(admin);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return avatarUrl;
    }
}