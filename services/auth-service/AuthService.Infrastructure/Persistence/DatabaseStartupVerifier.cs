// File: AuthService.Infrastructure/Persistence/DatabaseStartupVerifier.cs
// Purpose: Verifies database connectivity with exponential backoff retry logic
// Layer: Infrastructure

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AuthService.Infrastructure.Persistence;

public interface IDatabaseStartupVerifier
{
    Task<bool> VerifyAndWakeDatabaseAsync(AppDbContext context);
}

public class DatabaseStartupVerifier : IDatabaseStartupVerifier
{
    private readonly IDatabaseConnectionManager _connectionManager;
    private readonly ILogger<DatabaseStartupVerifier> _logger;

    public DatabaseStartupVerifier(
        IDatabaseConnectionManager connectionManager,
        ILogger<DatabaseStartupVerifier> logger)
    {
        _connectionManager = connectionManager;
        _logger = logger;
    }

    public async Task<bool> VerifyAndWakeDatabaseAsync(AppDbContext context)
    {
        var profile = _connectionManager.GetDetectedProfile();
        var maxRetries = _connectionManager.GetMaxRetryAttempts();
        var currentDelay = _connectionManager.GetInitialRetryDelay();

        _logger.LogInformation("Starting database verification. Detected Profile: {Profile}", profile);

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                var canConnect = await context.Database.CanConnectAsync();
                if (canConnect)
                {
                    _logger.LogInformation("Database connected successfully on attempt {Attempt}", attempt);
                    return true;
                }

                throw new Exception("Cannot connect to database");
            }
            catch (Exception ex)
            {
                var errorMsg = ex.ToString().ToLower();

                if (errorMsg.Contains("dns") || errorMsg.Contains("getaddrinfo"))
                {
                    _logger.LogError("DNS resolution failed - check connection string");
                }
                else if (errorMsg.Contains("ssl") || errorMsg.Contains("authentication"))
                {
                    _logger.LogError("SSL/Authentication failed - check credentials");
                }
                else
                {
                    _logger.LogWarning("Database connection attempt {Attempt}/{Max} failed. Server may be waking up.", attempt, maxRetries);
                }

                if (attempt < maxRetries)
                {
                    _logger.LogInformation("Retrying in {Seconds}s...", currentDelay.TotalSeconds);
                    await Task.Delay(currentDelay);
                    currentDelay = TimeSpan.FromMilliseconds(currentDelay.TotalMilliseconds * 1.5);
                }
            }
        }

        _logger.LogCritical("Failed to connect to database after {MaxRetries} attempts", maxRetries);
        return false;
    }
}