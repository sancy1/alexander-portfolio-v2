// File: AuthService.Application/Interfaces/Security/ITokenBlacklistService.cs
namespace AuthService.Application.Interfaces.Security;

public interface ITokenBlacklistService
{
    Task BlacklistTokenAsync(string token);
    Task<bool> IsTokenBlacklistedAsync(string token);
}