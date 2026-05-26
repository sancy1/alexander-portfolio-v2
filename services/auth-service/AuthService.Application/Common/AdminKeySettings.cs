// File: AuthService.Application/Common/AdminKeySettings.cs
// Purpose: Configuration settings for admin key validation
// Layer: Application

namespace AuthService.Application.Common;

public class AdminKeySettings
{
    public string MasterKey { get; set; } = string.Empty;
    public bool RequireForRegistration { get; set; } = true;
    public List<string> AllowedKeys { get; set; } = new();
}