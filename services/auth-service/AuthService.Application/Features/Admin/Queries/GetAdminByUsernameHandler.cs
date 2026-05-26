using MediatR;
using AuthService.Application.Interfaces.Persistence;

namespace AuthService.Application.Features.Admin.Queries;

public class GetAdminByUsernameHandler : IRequestHandler<GetAdminByUsernameQuery, global::AuthService.Domain.Entities.Admin?>
{
    private readonly IAdminRepository _adminRepository;

    public GetAdminByUsernameHandler(IAdminRepository adminRepository)
    {
        _adminRepository = adminRepository;
    }

    public async Task<global::AuthService.Domain.Entities.Admin?> Handle(GetAdminByUsernameQuery request, CancellationToken cancellationToken)
    {
        return await _adminRepository.GetByUsernameAsync(request.Username);
    }
}