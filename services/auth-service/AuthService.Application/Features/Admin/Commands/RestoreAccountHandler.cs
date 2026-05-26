using MediatR;
using AuthService.Application.Interfaces.Persistence;
using AuthService.Application.Interfaces.Security;

namespace AuthService.Application.Features.Admin.Commands;

public class RestoreAccountHandler : IRequestHandler<RestoreAccountCommand, bool>
{
    private readonly IAdminRepository _adminRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAdminKeyValidator _adminKeyValidator;

    public RestoreAccountHandler(
        IAdminRepository adminRepository,
        IUnitOfWork unitOfWork,
        IAdminKeyValidator adminKeyValidator)
    {
        _adminRepository = adminRepository;
        _unitOfWork = unitOfWork;
        _adminKeyValidator = adminKeyValidator;
    }

    public async Task<bool> Handle(RestoreAccountCommand request, CancellationToken cancellationToken)
    {
        // 1. Validate admin key
        if (!_adminKeyValidator.IsValidAdminKey(request.AdminKey))
        {
            return false;
        }

        // 2. Find admin by username
        var admin = await _adminRepository.GetByUsernameAsync(request.Username);
        
        if (admin == null)
        {
            return false;
        }

        // 3. Check if account is soft-deleted
        if (!admin.IsDeleted)
        {
            return false;
        }

        // 4. Check if still within 30-day window
        if (admin.PermanentDeleteAt.HasValue && admin.PermanentDeleteAt.Value <= DateTime.UtcNow)
        {
            return false;
        }

        // 5. Restore the account
        admin.Restore();
        _adminRepository.Update(admin);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}