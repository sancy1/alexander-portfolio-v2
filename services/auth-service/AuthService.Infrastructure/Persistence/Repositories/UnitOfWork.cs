
// File: AuthService.Infrastructure/Persistence/Repositories/UnitOfWork.cs
// Purpose: Unit of work implementation for transaction management
// Layer: Infrastructure

using AuthService.Application.Interfaces.Persistence;

namespace AuthService.Infrastructure.Persistence.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;

    public UnitOfWork(AppDbContext context)
    {
        _context = context;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }
}
