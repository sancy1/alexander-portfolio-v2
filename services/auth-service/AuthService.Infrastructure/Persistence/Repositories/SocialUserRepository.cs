// File: AuthService.Infrastructure/Persistence/Repositories/SocialUserRepository.cs
// Purpose: Repository implementation for social user operations
// Layer: Infrastructure

using Microsoft.EntityFrameworkCore;
using AuthService.Application.Interfaces.Persistence;
using AuthService.Domain.Entities;
using AuthService.Domain.Enums;

namespace AuthService.Infrastructure.Persistence.Repositories;

public class SocialUserRepository : ISocialUserRepository
{
    private readonly AppDbContext _context;

    public SocialUserRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<SocialUser?> GetByIdAsync(Guid id)
    {
        return await _context.SocialUsers
            .FirstOrDefaultAsync(u => u.Id == id);
    }

    public async Task<SocialUser?> GetByProviderIdAsync(string providerId, SocialProvider provider)
    {
        return await _context.SocialUsers
            .FirstOrDefaultAsync(u => u.ProviderId == providerId && u.Provider == provider);
    }

    public async Task<SocialUser?> GetByEmailAsync(string email)
    {
        return await _context.SocialUsers
            .FirstOrDefaultAsync(u => u.Email == email.ToLowerInvariant());
    }

    public async Task<bool> ExistsByEmailAsync(string email)
    {
        return await _context.SocialUsers
            .AnyAsync(u => u.Email == email.ToLowerInvariant());
    }

    public async Task<bool> ExistsByProviderIdAsync(string providerId, SocialProvider provider)
    {
        return await _context.SocialUsers
            .AnyAsync(u => u.ProviderId == providerId && u.Provider == provider);
    }

    public async Task AddAsync(SocialUser user)
    {
        await _context.SocialUsers.AddAsync(user);
    }

    public void Update(SocialUser user)
    {
        _context.SocialUsers.Update(user);
    }

    public void Delete(SocialUser user)
    {
        _context.SocialUsers.Remove(user);
    }

    public async Task<List<SocialUser>> GetDeletedUsersAsync()
    {
        return await _context.SocialUsers
            .Where(u => u.IsDeleted && u.PermanentDeleteAt <= DateTime.UtcNow)
            .ToListAsync();
    }



    // Add these new methods to the existing class

    public async Task<List<SocialUser>> GetAllAsync(int page, int pageSize, string? search = null, string? provider = null, bool? isBlocked = null, bool? isDeleted = null)
    {
        var query = _context.SocialUsers.AsQueryable();

        // Apply filters
        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(u => u.Email.Contains(search) || u.DisplayName.Contains(search));
        }

        if (!string.IsNullOrEmpty(provider))
        {
            if (Enum.TryParse<SocialProvider>(provider, true, out var providerEnum))
            {
                query = query.Where(u => u.Provider == providerEnum);
            }
        }

        if (isBlocked.HasValue)
        {
            query = query.Where(u => u.IsAdminBlocked == isBlocked.Value);
        }

        if (isDeleted.HasValue)
        {
            query = query.Where(u => u.IsDeleted == isDeleted.Value);
        }

        return await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetTotalCountAsync(string? search = null, string? provider = null, bool? isBlocked = null, bool? isDeleted = null)
    {
        var query = _context.SocialUsers.AsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(u => u.Email.Contains(search) || u.DisplayName.Contains(search));
        }

        if (!string.IsNullOrEmpty(provider))
        {
            if (Enum.TryParse<SocialProvider>(provider, true, out var providerEnum))
            {
                query = query.Where(u => u.Provider == providerEnum);
            }
        }

        if (isBlocked.HasValue)
        {
            query = query.Where(u => u.IsAdminBlocked == isBlocked.Value);
        }

        if (isDeleted.HasValue)
        {
            query = query.Where(u => u.IsDeleted == isDeleted.Value);
        }

        return await query.CountAsync();
    }

    public async Task<List<SocialUser>> GetBlockedUsersAsync()
    {
        return await _context.SocialUsers
            .Where(u => u.IsAdminBlocked == true)
            .OrderByDescending(u => u.AdminBlockedAt)
            .ToListAsync();
    }

    public async Task<List<SocialUser>> GetRecentlyActiveAsync(int days = 7)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-days);
        return await _context.SocialUsers
            .Where(u => u.LastLoginAt >= cutoffDate && u.IsDeleted == false)
            .OrderByDescending(u => u.LastLoginAt)
            .ToListAsync();
    }


}