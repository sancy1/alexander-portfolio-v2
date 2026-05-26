
// File: AuthService.Application/Interfaces/Security/IAdminKeyValidator.cs
// Purpose: Interface for admin key validation
// Layer: Application

namespace AuthService.Application.Interfaces.Security;

public interface IAdminKeyValidator
{
    bool IsValidAdminKey(string providedKey);
}
