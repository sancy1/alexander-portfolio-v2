using MediatR;
using AuthService.Application.Interfaces.Persistence;
using AuthService.Domain.Interfaces;

namespace AuthService.Application.Features.Admin.Commands;

public class ChangePasswordHandler : IRequestHandler<ChangePasswordCommand, bool>
{
    private readonly IAdminRepository _adminRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;

    public ChangePasswordHandler(
        IAdminRepository adminRepository,
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher)
    {
        _adminRepository = adminRepository;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
    }

    public async Task<bool> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        var admin = await _adminRepository.GetByIdAsync(request.AdminId);
        
        if (admin == null)
        {
            return false;
        }

        // Verify current password
        if (!_passwordHasher.VerifyPassword(request.CurrentPassword, admin.PasswordHash))
        {
            return false;
        }

        // Hash new password and update
        var newPasswordHash = _passwordHasher.HashPassword(request.NewPassword);
        admin.ChangePassword(newPasswordHash);
        
        _adminRepository.Update(admin);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}