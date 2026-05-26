
// File: AuthService.Application/Interfaces/Persistence/IUnitOfWork.cs
// Purpose: Unit of work pattern for transaction management
// Layer: Application

namespace AuthService.Application.Interfaces.Persistence;

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
