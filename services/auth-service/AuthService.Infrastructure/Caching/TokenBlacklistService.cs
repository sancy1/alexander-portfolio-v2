// File: AuthService.Infrastructure/Caching/TokenBlacklistService.cs
using AuthService.Application.Interfaces.Security;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;

namespace AuthService.Infrastructure.Caching;

public class TokenBlacklistService : ITokenBlacklistService
{
    private readonly IRedisCacheService _redisCacheService;
    private readonly ILogger<TokenBlacklistService> _logger;
    private readonly TimeSpan _blacklistDuration = TimeSpan.FromDays(30);

    public TokenBlacklistService(
        IRedisCacheService redisCacheService,
        ILogger<TokenBlacklistService> logger)
    {
        _redisCacheService = redisCacheService;
        _logger = logger;
    }

    public async Task<bool> IsTokenBlacklistedAsync(string token)
    {
        try
        {
            var tokenHash = ComputeHash(token);
            var key = $"blacklist:{tokenHash}";
            var result = await _redisCacheService.GetAsync<string>(key);
            return result != null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis blacklist check failed - allowing request through");
            return false;
        }
    }

    public async Task BlacklistTokenAsync(string token)
    {
        try
        {
            var tokenHash = ComputeHash(token);
            var key = $"blacklist:{tokenHash}";
            await _redisCacheService.SetAsync(key, "revoked", _blacklistDuration);
            _logger.LogInformation("Token blacklisted successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to blacklist token in Redis");
        }
    }

    private static string ComputeHash(string token)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(token);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}