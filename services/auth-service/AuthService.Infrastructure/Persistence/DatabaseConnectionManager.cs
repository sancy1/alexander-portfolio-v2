// File: AuthService.Infrastructure/Persistence/DatabaseConnectionManager.cs
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
    private readonly string _cachedConnectionString; // ← cache it once

    public DatabaseConnectionManager(
        IOptions<DatabaseSettings> options,
        ILogger<DatabaseConnectionManager>? logger = null)
    {
        _settings = options.Value;
        _logger = logger;
        _profile = DetectProfile(_settings.ConnectionString);

        // Log ONCE at construction — never again
        _logger?.LogInformation("Detected database profile: {Profile}", _profile);

        // Build and cache the connection string once at startup
        _cachedConnectionString = BuildConnectionString();
        _logger?.LogInformation("Database connection string built for profile: {Profile}", _profile);
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

    // Returns cached string — no logging, no rebuilding on every call
    public string BuildOptimizedConnectionString() => _cachedConnectionString;

    private string BuildConnectionString()
    {
        var rawUrl = _settings.ConnectionString;
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

        return builder.ToString();
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
        if (url.Contains("Host=") || url.Contains("Server="))
            return url;

        if (url.StartsWith("postgresql://") || url.StartsWith("postgres://"))
        {
            try
            {
                var withoutProtocol = url
                    .Replace("postgresql://", "")
                    .Replace("postgres://", "");

                var atIndex = withoutProtocol.IndexOf('@');
                if (atIndex > 0)
                {
                    var userPass = withoutProtocol.Substring(0, atIndex);
                    var hostDb = withoutProtocol.Substring(atIndex + 1);

                    var userPassParts = userPass.Split(':');
                    var username = userPassParts[0];
                    var password = userPassParts.Length > 1 ? userPassParts[1] : "";

                    var slashIndex = hostDb.IndexOf('/');
                    if (slashIndex > 0)
                    {
                        var hostPart = hostDb.Substring(0, slashIndex);
                        var database = hostDb.Substring(slashIndex + 1);

                        var queryIndex = database.IndexOf('?');
                        if (queryIndex > 0)
                            database = database.Substring(0, queryIndex);

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