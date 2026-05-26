namespace AuthService.Application.Interfaces.Security;

public interface ITokenBlacklistService
{
    Task BlacklistTokenAsync(string token, DateTime expiry);
    Task<bool> IsTokenBlacklistedAsync(string token);
}