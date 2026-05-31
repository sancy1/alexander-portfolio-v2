using MediatR;
using System.Threading;
using System.Threading.Tasks;
using AuthService.Application.DTOs.Responses;
using AuthService.Application.Interfaces.Persistence;

namespace AuthService.Application.Features.Admin.Queries;

public class GetAdminProfileHandler : IRequestHandler<GetAdminProfileQuery, AdminProfileResponse?>
{
    private readonly IAdminRepository _adminRepository;

    public GetAdminProfileHandler(IAdminRepository adminRepository)
    {
        _adminRepository = adminRepository;
    }

    public async Task<AdminProfileResponse?> Handle(GetAdminProfileQuery request, CancellationToken cancellationToken)
    {
        var admin = await _adminRepository.GetByIdAsync(request.AdminId);
        
        if (admin == null)
        {
            return null;
        }

        return new AdminProfileResponse
        {
            Id = admin.Id,
            Username = admin.Username,
            Email = admin.Email,
            Role = admin.Role.ToString(),
            CreatedAt = admin.CreatedAt,
            LastLoginAt = admin.LastLoginAt,
            UpdatedAt = admin.UpdatedAt,
            AvatarUrl = admin.AvatarUrl,  // 🔐 Fixed: Added missing comma here to prevent compilation crash!

            // Map the newly integrated database parameters to the response context contract
            FullName = admin.FullName,
            JobTitle = admin.JobTitle,
            Headline = admin.Headline,
            Tagline = admin.Tagline,
            Bio = admin.Bio,
            Phone = admin.Phone,
            Location = admin.Location,
            Website = admin.Website,
            SocialLinks = admin.SocialLinks
        };
    }
}
