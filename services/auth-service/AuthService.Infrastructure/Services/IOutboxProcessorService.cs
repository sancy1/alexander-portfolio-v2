// File: services/auth-service/AuthService.Infrastructure/Services/IOutboxProcessorService.cs
using System.Threading.Tasks;

namespace AuthService.Infrastructure.Services;

public interface IOutboxProcessorService
{
    Task<int> ProcessPendingMessagesAsync(int maxMessages = 10);
    Task<int> GetPendingCountAsync();
    
    // 👇 FIXED: Aligned method name with your concrete OutboxProcessorService implementation
    Task CleanupProcessedMessagesAsync(int daysToKeep = 7);
}
