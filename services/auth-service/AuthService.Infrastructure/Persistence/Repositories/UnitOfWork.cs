// File: services/auth-service/AuthService.Infrastructure/Persistence/Repositories/UnitOfWork.cs
// Purpose: Unit of work implementation with transaction management
// Layer: Infrastructure

using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Storage;
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

    // 👇 FIX: Implements transaction boundaries wrapping EF Core's native storage broker
    public async Task<IDbTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        var efTransaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        return new EntityFrameworkDbTransaction(efTransaction);
    }
}

/// <summary>
/// Infrastructure adapter wrapper translating EF Core transaction blocks to the Application core layer contract.
/// </summary>
internal class EntityFrameworkDbTransaction : IDbTransaction
{
    private readonly IDbContextTransaction _efTransaction;

    public EntityFrameworkDbTransaction(IDbContextTransaction efTransaction)
    {
        _efTransaction = efTransaction;
    }

    public Task CommitAsync(CancellationToken cancellationToken = default)
    {
        return _efTransaction.CommitAsync(cancellationToken);
    }

    public Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        return _efTransaction.RollbackAsync(cancellationToken);
    }

    public void Dispose()
    {
        _efTransaction.Dispose();
    }

    public ValueTask DisposeAsync()
    {
        return _efTransaction.DisposeAsync();
    }
}
