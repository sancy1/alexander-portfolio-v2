
// File: AuthService.Infrastructure/Security/AdminKeyValidator.cs
// Purpose: Validates admin keys for registration from .env
// Layer: Infrastructure

using Microsoft.Extensions.Options;
using AuthService.Application.Common;
using AuthService.Application.Interfaces.Security;

namespace AuthService.Infrastructure.Security;

public class AdminKeyValidator : IAdminKeyValidator
{
    private readonly AdminKeySettings _settings;

    public AdminKeyValidator(IOptions<AdminKeySettings> settings)
    {
        _settings = settings.Value;
    }

    public bool IsValidAdminKey(string providedKey)
    {
        if (string.IsNullOrWhiteSpace(providedKey))
            return false;

        // Check master key
        if (!string.IsNullOrEmpty(_settings.MasterKey) && _settings.MasterKey == providedKey)
            return true;

        // Check allowed keys list
        if (_settings.AllowedKeys != null && _settings.AllowedKeys.Contains(providedKey))
            return true;

        return false;
    }
}
