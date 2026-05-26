// File: AuthService.Infrastructure/Persistence/Repositories/OutboxRepository.cs
// Purpose: Repository implementation for outbox message operations
// Layer: Infrastructure

using Microsoft.EntityFrameworkCore;
using AuthService.Application.Interfaces.Persistence;
using AuthService.Domain.Entities;

namespace AuthService.Infrastructure.Persistence.Repositories;

public class OutboxRepository : IOutboxRepository
{
    private readonly AppDbContext _context;

    public OutboxRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(OutboxMessage message)
    {
        await _context.OutboxMessages.AddAsync(message);
    }

    public async Task<List<OutboxMessage>> GetUnprocessedMessagesAsync(int maxCount = 10)
    {
        return await _context.OutboxMessages
            .Where(m => m.ProcessedAt == null && m.RetryCount < 3)
            .OrderBy(m => m.CreatedAt)
            .Take(maxCount)
            .ToListAsync();
    }

    public async Task UpdateAsync(OutboxMessage message)
    {
        _context.OutboxMessages.Update(message);
        await Task.CompletedTask;
    }

    public async Task<int> GetPendingCountAsync()
    {
        return await _context.OutboxMessages
            .CountAsync(m => m.ProcessedAt == null && m.RetryCount < 3);
    }

    public async Task CleanupProcessedMessagesAsync(int daysToKeep = 7)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-daysToKeep);
        var oldMessages = await _context.OutboxMessages
            .Where(m => m.ProcessedAt != null && m.ProcessedAt < cutoffDate)
            .Take(1000)
            .ToListAsync();
        
        _context.OutboxMessages.RemoveRange(oldMessages);
    }
}