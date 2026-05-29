// File: services/auth-service/AuthService.Application/Interfaces/Persistence/IUnitOfWork.cs
// Purpose: Unit of work pattern with transaction boundary management
// Layer: Application

using System;
using System.Threading;
using System.Threading.Tasks;

namespace AuthService.Application.Interfaces.Persistence;

public interface IDbTransaction : IDisposable, IAsyncDisposable
{
    Task CommitAsync(CancellationToken cancellationToken = default);
    Task RollbackAsync(CancellationToken cancellationToken = default);
}

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    
    // 👇 CRITICAL ADDITION: Enforces transaction isolation management across handlers
    Task<IDbTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
}
