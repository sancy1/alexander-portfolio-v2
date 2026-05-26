// File: AuthService.Application/Interfaces/Persistence/IOutboxRepository.cs
// Purpose: Repository interface for outbox message operations
// Layer: Application

using AuthService.Domain.Entities;

namespace AuthService.Application.Interfaces.Persistence;

public interface IOutboxRepository
{
    Task AddAsync(OutboxMessage message);
    Task<List<OutboxMessage>> GetUnprocessedMessagesAsync(int maxCount = 10);
    Task UpdateAsync(OutboxMessage message);
    Task<int> GetPendingCountAsync();
    Task CleanupProcessedMessagesAsync(int daysToKeep = 7);
}