namespace AuthService.Application.DTOs.Responses;

public class DeleteAccountResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime? PermanentDeleteDate { get; set; }
    public bool IsReversible { get; set; }
}