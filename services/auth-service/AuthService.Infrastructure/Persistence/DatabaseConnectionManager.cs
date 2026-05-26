// File: AuthService.Infrastructure/Persistence/DatabaseConnectionManager.cs
// Purpose: Detects cloud provider and optimizes connection strings dynamically
// Layer: Infrastructure

using System.Data.Common;
using AuthService.Application.Common;
using Microsoft.Extensions.Options;
using Npgsql;
using Microsoft.Extensions.Logging;

namespace AuthService.Infrastructure.Persistence;

public interface IDatabaseConnectionManager
{
    string BuildOptimizedConnectionString();
    DatabaseProfile GetDetectedProfile();
    int GetMaxRetryAttempts();
    TimeSpan GetInitialRetryDelay();
}

public class DatabaseConnectionManager : IDatabaseConnectionManager
{
    private readonly DatabaseSettings _settings;
    private readonly DatabaseProfile _profile;
    private readonly ILogger<DatabaseConnectionManager>? _logger;

    public DatabaseConnectionManager(IOptions<DatabaseSettings> options, ILogger<DatabaseConnectionManager>? logger = null)
    {
        _settings = options.Value;
        _logger = logger;
        _profile = DetectProfile(_settings.ConnectionString);
        
        _logger?.LogInformation("Detected database profile: {Profile}", _profile);
        _logger?.LogInformation("Raw connection string: {ConnectionString}", 
            _settings.ConnectionString.Substring(0, Math.Min(100, _settings.ConnectionString.Length)) + "...");
    }

    public DatabaseProfile GetDetectedProfile() => _profile;

    public int GetMaxRetryAttempts()
    {
        if (_settings.OverrideMaxRetries > 0) return _settings.OverrideMaxRetries;
        
        return _profile switch
        {
            DatabaseProfile.ServerlessPooler => 6,
            DatabaseProfile.CloudProxy => 4,
            _ => 3
        };
    }

    public TimeSpan GetInitialRetryDelay()
    {
        return _profile switch
        {
            DatabaseProfile.ServerlessPooler => TimeSpan.FromSeconds(2.5),
            _ => TimeSpan.FromSeconds(1.5)
        };
    }

    public string BuildOptimizedConnectionString()
    {
        var rawUrl = _settings.ConnectionString;
        
        // Parse the connection string to Npgsql format
        var parsedConnectionString = ParsePostgresUrlToNpgsql(rawUrl);
        
        var builder = new NpgsqlConnectionStringBuilder(parsedConnectionString);

        switch (_profile)
        {
            case DatabaseProfile.ServerlessPooler:
                builder.Pooling = true;
                builder.MinPoolSize = 1;
                builder.MaxPoolSize = 5;
                builder.SslMode = SslMode.Require;
                builder.TrustServerCertificate = true;
                break;

            case DatabaseProfile.EnterpriseInstance:
                builder.Pooling = true;
                builder.MinPoolSize = 5;
                builder.MaxPoolSize = _settings.DefaultPoolSize;
                builder.SslMode = SslMode.Require;
                break;

            case DatabaseProfile.LocalDocker:
                builder.Pooling = true;
                builder.MinPoolSize = 1;
                builder.MaxPoolSize = _settings.DefaultPoolSize;
                builder.SslMode = SslMode.Prefer;
                break;
        }

        builder.CommandTimeout = 30;
        builder.Timeout = 15;
        builder.ConnectionIdleLifetime = 300;

        var result = builder.ToString();
        _logger?.LogInformation("Built connection string (host): {Host}", 
            result.Split(';').FirstOrDefault(s => s.StartsWith("Host=")));
        
        return result;
    }

    private static DatabaseProfile DetectProfile(string connectionString)
    {
        var lowerStr = connectionString.ToLower();

        if (lowerStr.Contains("neon.tech") || lowerStr.Contains("supabase.co"))
            return DatabaseProfile.ServerlessPooler;

        if (lowerStr.Contains("amazonaws.com") || lowerStr.Contains("azure.com"))
            return DatabaseProfile.EnterpriseInstance;

        if (lowerStr.Contains("/cloudsql/") || lowerStr.Contains("google"))
            return DatabaseProfile.CloudProxy;

        return DatabaseProfile.LocalDocker;
    }

    private static string ParsePostgresUrlToNpgsql(string url)
    {
        // If it's already in Npgsql format
        if (url.Contains("Host=") || url.Contains("Server="))
            return url;

        // Handle postgresql:// URLs
        if (url.StartsWith("postgresql://") || url.StartsWith("postgres://"))
        {
            try
            {
                // Remove the protocol
                var withoutProtocol = url.Replace("postgresql://", "").Replace("postgres://", "");
                
                // Split user:pass from host/db
                var atIndex = withoutProtocol.IndexOf('@');
                if (atIndex > 0)
                {
                    var userPass = withoutProtocol.Substring(0, atIndex);
                    var hostDb = withoutProtocol.Substring(atIndex + 1);
                    
                    // Split user and password
                    var userPassParts = userPass.Split(':');
                    var username = userPassParts[0];
                    var password = userPassParts.Length > 1 ? userPassParts[1] : "";
                    
                    // Split host and database
                    var slashIndex = hostDb.IndexOf('/');
                    if (slashIndex > 0)
                    {
                        var hostPart = hostDb.Substring(0, slashIndex);
                        var database = hostDb.Substring(slashIndex + 1);
                        
                        // Remove query parameters from database name
                        var queryIndex = database.IndexOf('?');
                        if (queryIndex > 0)
                            database = database.Substring(0, queryIndex);
                        
                        // Parse host and port
                        var host = hostPart;
                        var port = 5432;
                        var colonIndex = hostPart.IndexOf(':');
                        if (colonIndex > 0)
                        {
                            host = hostPart.Substring(0, colonIndex);
                            port = int.Parse(hostPart.Substring(colonIndex + 1));
                        }
                        
                        return $"Host={host};Port={port};Database={database};Username={username};Password={password};SslMode=Require;";
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing connection string: {ex.Message}");
                return url;
            }
        }
        
        return url;
    }
}