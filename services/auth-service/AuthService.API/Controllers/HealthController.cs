// File: services/auth-service/AuthService.API/Controllers/HealthController.cs
// Purpose: Health check endpoints for service, database, RabbitMQ, and Kafka monitoring
// Layer: API

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using AuthService.Infrastructure.Persistence;
using AuthService.Application.Interfaces.Messaging;
using AuthService.Infrastructure.Messaging.Kafka;
using AuthService.Infrastructure.Messaging.RabbitMQ;
using RabbitMQ.Client;

namespace AuthService.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class HealthController : ControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<HealthController> _logger;
    private readonly IServiceProvider _serviceProvider; // 👇 Added to cleanly support your modified settings pattern

    public HealthController(
        AppDbContext dbContext, 
        ILogger<HealthController> logger,
        IServiceProvider serviceProvider)
    {
        _dbContext = dbContext;
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// Basic health check - returns service status
    /// </summary>
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new
        {
            status = "healthy",
            service = "AuthService",
            timestamp = DateTime.UtcNow,
            version = "1.0.0"
        });
    }

    /// <summary>
    /// Simple ping endpoint - tests if service is responding
    /// </summary>
    [HttpGet("ping")]
    public IActionResult Ping()
    {
        return Ok(new { message = "pong", timestamp = DateTime.UtcNow });
    }

    /// <summary>
    /// Test RabbitMQ connection dynamically using modern async scopes
    /// </summary>
    [HttpGet("test/rabbitmq")]
    public async Task<IActionResult> TestRabbitMQ()
    {
        try
        {
            var rabbitMQSettings = _serviceProvider.GetService<IOptions<RabbitMQSettings>>();
            if (rabbitMQSettings?.Value == null)
            {
                return Ok(new { status = "unconfigured", message = "RabbitMQ not configured" });
            }

            var factory = new ConnectionFactory
            {
                HostName = rabbitMQSettings.Value.HostName,
                Port = rabbitMQSettings.Value.Port,
                UserName = rabbitMQSettings.Value.UserName,
                Password = rabbitMQSettings.Value.Password,
                VirtualHost = rabbitMQSettings.Value.VirtualHost,
                // 👇 Added to prevent TLS handshakes from crashing on CloudAMQP
                Ssl = rabbitMQSettings.Value.UseSsl ? new SslOption { Enabled = true, ServerName = rabbitMQSettings.Value.HostName } : null
            };

            // 👇 Enforce modern asynchronous using blocks to guarantee zero memory leaks
            await using var connection = await factory.CreateConnectionAsync();
            await using var channel = await connection.CreateChannelAsync();
            
            return Ok(new { status = "healthy", message = "RabbitMQ connection successful" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RabbitMQ connectivity test failed");
            return StatusCode(500, new { status = "unhealthy", error = ex.Message });
        }
    }

    /// <summary>
    /// Test Kafka publishing - Preserved exactly as originally requested
    /// </summary>
    [HttpGet("test/kafka")]
    public async Task<IActionResult> TestKafka()
    {
        try
        {
            var producer = HttpContext.RequestServices.GetRequiredService<IKafkaProducer>();
            await producer.ProduceAuditLogAsync(new 
            { 
                eventType = "test.event",
                message = "Hello from AuthService",
                timestamp = DateTime.UtcNow,
                service = "auth-service"
            });
            
            return Ok(new { message = "Kafka message sent successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Kafka test generation event failed");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Database connectivity check - Fixed to prevent resource pooling deadlocks
    /// </summary>
    [HttpGet("db")]
    public async Task<IActionResult> CheckDatabase()
    {
        try
        {
            var canConnect = await _dbContext.Database.CanConnectAsync();
            
            if (canConnect)
            {
                var connection = _dbContext.Database.GetDbConnection();
                
                // 👇 Enforce try-finally structural wrapper to guarantee connection closure on Neon PostgreSQL
                await _dbContext.Database.OpenConnectionAsync();
                try
                {
                    await using var command = connection.CreateCommand();
                    command.CommandText = "SELECT 1";
                    var result = await command.ExecuteScalarAsync();
                    
                    return Ok(new
                    {
                        status = "healthy",
                        database = "connected",
                        timestamp = DateTime.UtcNow,
                        testQuery = result?.ToString() == "1" ? "success" : "failed"
                    });
                }
                finally
                {
                    await _dbContext.Database.CloseConnectionAsync();
                }
            }
            
            return StatusCode(503, new
            {
                status = "unhealthy",
                database = "disconnected",
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database health check failed");
            return StatusCode(503, new
            {
                status = "unhealthy",
                database = "error",
                error = ex.Message,
                timestamp = DateTime.UtcNow
            });
        }
    }
}
