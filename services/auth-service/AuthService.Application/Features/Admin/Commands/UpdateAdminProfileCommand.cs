using MediatR;
using AuthService.Application.DTOs.Responses;

namespace AuthService.Application.Features.Admin.Commands;

public class UpdateAdminProfileCommand : IRequest<UpdateAdminProfileResponse>
{
    public Guid AdminId { get; set; }
    public string? Username { get; set; }
    public string? Email { get; set; }

    public UpdateAdminProfileCommand(Guid adminId, string? username, string? email)
    {
        AdminId = adminId;
        Username = username;
        Email = email;
    }
}