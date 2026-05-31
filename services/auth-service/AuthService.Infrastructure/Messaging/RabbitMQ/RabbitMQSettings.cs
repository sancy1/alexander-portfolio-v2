
// File: services/auth-service/AuthService.Infrastructure/Messaging/RabbitMQSettings.cs

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace AuthService.Infrastructure.Messaging.RabbitMQ;

public class RabbitMQSettings
{
    public string HostName { get; set; } = "localhost";
    public int Port { get; set; } = 5671; 
    public string UserName { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string VirtualHost { get; set; } = "/"; 
    
    public int ConnectionTimeout { get; set; } = 30;
    public int RetryCount { get; set; } = 5;
    public int RetryDelayMilliseconds { get; set; } = 2000;
    public bool UseSsl { get; set; } = true; 
    public string SslServerName { get; set; } = "";
    
    public string UserEventsExchange { get; set; } = "user.events";
    public string AdminEventsExchange { get; set; } = "admin.events";
    public string SecurityEventsExchange { get; set; } = "security.events";
    public string AuthEventsQueue { get; set; } = "auth_service_queue";
    
    public bool IsValid => 
        !string.IsNullOrEmpty(HostName) && 
        HostName != "localhost" &&
        UserName != "guest" &&
        VirtualHost != "/" && 
        !string.IsNullOrEmpty(Password);
}

// 👇 The Connection Manager class is now embedded here to guarantee compilation visibility
public class RabbitMQConnectionManager : IAsyncDisposable
{
    private readonly ConnectionFactory _factory;
    private readonly ILogger<RabbitMQConnectionManager> _logger;
    private IConnection? _connection;
    private readonly SemaphoreSlim _connectionLock = new(1, 1);
    private bool _disposed;

    public RabbitMQConnectionManager(IOptions<RabbitMQSettings> settings, ILogger<RabbitMQConnectionManager> logger)
    {
        _logger = logger;
        var s = settings.Value;

        _factory = new ConnectionFactory
        {
            HostName = s.HostName,
            Port = s.Port,
            UserName = s.UserName,
            Password = s.Password,
            VirtualHost = s.VirtualHost,
            AutomaticRecoveryEnabled = true,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(10),
            RequestedHeartbeat = TimeSpan.FromSeconds(60)
        };

        if (s.UseSsl)
        {
            _factory.Ssl = new SslOption
            {
                Enabled = true,
                ServerName = s.HostName
            };
        }
    }

    public async Task<IConnection> GetConnectionAsync()
    {
        if (_connection is { IsOpen: true }) return _connection;

        await _connectionLock.WaitAsync();
        try
        {
            if (_connection is { IsOpen: true }) return _connection;

            _logger.LogInformation("Establishing singleton connection to CloudAMQP...");
            _connection = await _factory.CreateConnectionAsync();
            return _connection;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to CloudAMQP broker.");
            throw;
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        if (_connection != null) await _connection.CloseAsync();
        _connectionLock.Dispose();
        _disposed = true;
    }
}
