// File: AuthService.Infrastructure/Persistence/Repositories/AdminRepository.cs
// Purpose: Repository implementation for admin operations
// Layer: Infrastructure

using Microsoft.EntityFrameworkCore;
using AuthService.Application.Interfaces.Persistence;
using AuthService.Domain.Entities;

namespace AuthService.Infrastructure.Persistence.Repositories;

public class AdminRepository : IAdminRepository
{
    private readonly AppDbContext _context;

    public AdminRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Admin?> GetByIdAsync(Guid id)
    {
        return await _context.Admins.FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task<Admin?> GetByUsernameAsync(string username)
    {
        return await _context.Admins.FirstOrDefaultAsync(a => a.Username == username);
    }

    public async Task<Admin?> GetByEmailAsync(string email)
    {
        return await _context.Admins.FirstOrDefaultAsync(a => a.Email == email);
    }

    public async Task AddAsync(Admin admin)
    {
        await _context.Admins.AddAsync(admin);
    }

    public void Update(Admin admin)
    {
        _context.Admins.Update(admin);
    }

    public async Task<bool> ExistsByUsernameAsync(string username)
    {
        return await _context.Admins.AnyAsync(a => a.Username == username);
    }

    public async Task<bool> ExistsByEmailAsync(string email)
    {
        return await _context.Admins.AnyAsync(a => a.Email == email);
    }

    public void Delete(Admin admin)
    {
        _context.Admins.Remove(admin);
    }
    
}