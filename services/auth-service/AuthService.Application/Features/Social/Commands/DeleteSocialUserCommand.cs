// File: AuthService.Application/Features/Social/Commands/DeleteSocialUserCommand.cs
// Purpose: Command for social user self-deletion
// Layer: Application

using MediatR;
using AuthService.Application.DTOs.Responses;

namespace AuthService.Application.Features.Social.Commands;

public class DeleteSocialUserCommand : IRequest<DeleteAccountResponse>
{
    public Guid UserId { get; set; }
    public string ConfirmEmail { get; set; }
    public bool PermanentDelete { get; set; }
    public string? Reason { get; set; }

    public DeleteSocialUserCommand(Guid userId, string confirmEmail, bool permanentDelete, string? reason)
    {
        UserId = userId;
        ConfirmEmail = confirmEmail;
        PermanentDelete = permanentDelete;
        Reason = reason;
    }
}