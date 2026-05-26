// File: AuthService.Application/Interfaces/Persistence/ISocialUserRepository.cs
// Purpose: Repository interface for social user operations
// Layer: Application

using AuthService.Domain.Entities;
using AuthService.Domain.Enums;

namespace AuthService.Application.Interfaces.Persistence;

public interface ISocialUserRepository
{
    Task<SocialUser?> GetByIdAsync(Guid id);
    Task<SocialUser?> GetByProviderIdAsync(string providerId, SocialProvider provider);
    Task<SocialUser?> GetByEmailAsync(string email);
    Task<bool> ExistsByEmailAsync(string email);
    Task<bool> ExistsByProviderIdAsync(string providerId, SocialProvider provider);
    Task AddAsync(SocialUser user);
    void Update(SocialUser user);
    void Delete(SocialUser user);
    Task<List<SocialUser>> GetDeletedUsersAsync();

    // Add these new methods to the existing interface
    Task<List<SocialUser>> GetAllAsync(int page, int pageSize, string? search = null, string? provider = null, bool? isBlocked = null, bool? isDeleted = null);
    Task<int> GetTotalCountAsync(string? search = null, string? provider = null, bool? isBlocked = null, bool? isDeleted = null);
    Task<List<SocialUser>> GetBlockedUsersAsync();
    Task<List<SocialUser>> GetRecentlyActiveAsync(int days = 7);

}