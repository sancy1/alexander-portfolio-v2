using Microsoft.Extensions.Caching.Distributed;
using AuthService.Application.Interfaces.Security;

namespace AuthService.Infrastructure.Caching;

public class TokenBlacklistService : ITokenBlacklistService
{
    private readonly IDistributedCache _cache;

    public TokenBlacklistService(IDistributedCache cache)
    {
        _cache = cache;
    }

    public async Task BlacklistTokenAsync(string token, DateTime expiry)
    {
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpiration = expiry
        };
        
        await _cache.SetStringAsync($"blacklist:{token}", "revoked", options);
    }

    public async Task<bool> IsTokenBlacklistedAsync(string token)
    {
        var result = await _cache.GetStringAsync($"blacklist:{token}");
        return result != null;
    }
}