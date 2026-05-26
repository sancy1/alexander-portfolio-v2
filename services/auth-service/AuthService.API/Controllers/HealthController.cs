// File: AuthService.API/Controllers/HealthController.cs
// Purpose: Health check endpoints for service and database monitoring
// Layer: API

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AuthService.Infrastructure.Persistence;

using AuthService.Application.Interfaces.Messaging;
using AuthService.Infrastructure.Messaging.Kafka;

namespace AuthService.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class HealthController : ControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<HealthController> _logger;

    public HealthController(AppDbContext dbContext, ILogger<HealthController> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
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
    /// Test RabbitMQ publishing
    /// </summary>
    [HttpGet("test/rabbitmq")]
    public async Task<IActionResult> TestRabbitMQ()
    {
        try
        {
            var publisher = HttpContext.RequestServices.GetRequiredService<IMessagePublisher>();
            await publisher.PublishAsync("test.message", new 
            { 
                message = "Hello from AuthService",
                timestamp = DateTime.UtcNow,
                type = "test"
            });
            
            return Ok(new { message = "RabbitMQ message sent successfully" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Test Kafka publishing
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
            return StatusCode(500, new { error = ex.Message });
        }
    }




    /// <summary>
    /// Database connectivity check - tests if database is reachable
    /// </summary>
    [HttpGet("db")]
    public async Task<IActionResult> CheckDatabase()
    {
        try
        {
            // Try a simple query to check database connectivity
            var canConnect = await _dbContext.Database.CanConnectAsync();
            
            if (canConnect)
            {
                // Execute a simple query to verify full functionality
                using var command = _dbContext.Database.GetDbConnection().CreateCommand();
                command.CommandText = "SELECT 1";
                await _dbContext.Database.OpenConnectionAsync();
                var result = await command.ExecuteScalarAsync();
                await _dbContext.Database.CloseConnectionAsync();
                
                return Ok(new
                {
                    status = "healthy",
                    database = "connected",
                    timestamp = DateTime.UtcNow,
                    testQuery = result?.ToString() == "1" ? "success" : "failed"
                });
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