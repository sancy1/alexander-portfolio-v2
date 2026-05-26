// File: AuthService.Application/Features/Admin/Commands/AdminDeleteSocialUserCommand.cs
// Purpose: Command for admin to forcibly delete social user account
// Layer: Application

using MediatR;
using AuthService.Application.DTOs.Responses;

namespace AuthService.Application.Features.Admin.Commands;

public class AdminDeleteSocialUserCommand : IRequest<DeleteAccountResponse>
{
    public Guid UserId { get; set; }
    public string Reason { get; set; }
    public bool PermanentDelete { get; set; }

    public AdminDeleteSocialUserCommand(Guid userId, string reason, bool permanentDelete)
    {
        UserId = userId;
        Reason = reason;
        PermanentDelete = permanentDelete;
    }
}