namespace AuthService.Application.DTOs.Requests;

public class DeleteAccountRequest
{
    public string ConfirmUsername { get; set; } = string.Empty;
    public bool PermanentDelete { get; set; } = false;
    public string? Reason { get; set; }
}