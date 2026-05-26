using MediatR;
using AuthService.Application.DTOs.Responses;

namespace AuthService.Application.Features.Admin.Commands;

public class DeleteAccountCommand : IRequest<DeleteAccountResponse>
{
    public Guid AdminId { get; set; }
    public string ConfirmUsername { get; set; }
    public bool PermanentDelete { get; set; }
    public string? Reason { get; set; }

    public DeleteAccountCommand(Guid adminId, string confirmUsername, bool permanentDelete, string? reason)
    {
        AdminId = adminId;
        ConfirmUsername = confirmUsername;
        PermanentDelete = permanentDelete;
        Reason = reason;
    }
}