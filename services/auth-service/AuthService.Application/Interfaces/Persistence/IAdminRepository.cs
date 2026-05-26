// File: AuthService.Application/Interfaces/Persistence/IAdminRepository.cs
// Purpose: Repository interface for admin operations
// Layer: Application

using AuthService.Domain.Entities;

namespace AuthService.Application.Interfaces.Persistence;

public interface IAdminRepository
{
    Task<Admin?> GetByIdAsync(Guid id);
    Task<Admin?> GetByUsernameAsync(string username);
    Task<Admin?> GetByEmailAsync(string email);
    Task AddAsync(Admin admin);
    void Update(Admin admin);
    Task<bool> ExistsByUsernameAsync(string username);
    Task<bool> ExistsByEmailAsync(string email);
    void Delete(Admin admin); 
}