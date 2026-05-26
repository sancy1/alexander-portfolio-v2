// File: AuthService.Application/Interfaces/Services/IOutboxProcessorService.cs
// Purpose: Interface for outbox message processing
// Layer: Application

namespace AuthService.Application.Interfaces.Services;

public interface IOutboxProcessorService
{
    Task<int> ProcessPendingMessagesAsync(int maxMessages = 10);
    Task<int> GetPendingCountAsync();
    Task CleanupOldMessagesAsync(int daysToKeep = 7);
}