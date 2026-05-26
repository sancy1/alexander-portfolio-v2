using MediatR;
using AuthService.Application.DTOs.Responses;

namespace AuthService.Application.Features.Admin.Queries;

public class GetAdminProfileQuery : IRequest<AdminProfileResponse?>
{
    public Guid AdminId { get; set; }

    public GetAdminProfileQuery(Guid adminId)
    {
        AdminId = adminId;
    }
}