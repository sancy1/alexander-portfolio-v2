// File: AuthService.API/Controllers/OutboxController.cs
// Purpose: Endpoints for monitoring and manually processing outbox
// Layer: API

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AuthService.Application.Interfaces.Services;  // UPDATED using

namespace AuthService.API.Controllers;

[ApiController]
[Route("api/v1/admin/outbox")]
[Authorize(Roles = "Admin,SuperAdmin")]
public class OutboxController : ControllerBase
{
    private readonly IOutboxProcessorService _outboxProcessor;
    private readonly ILogger<OutboxController> _logger;

    public OutboxController(IOutboxProcessorService outboxProcessor, ILogger<OutboxController> logger)
    {
        _outboxProcessor = outboxProcessor;
        _logger = logger;
    }

    /// <summary>
    /// Get pending outbox message count
    /// </summary>
    [HttpGet("pending")]
    public async Task<IActionResult> GetPendingCount()
    {
        var count = await _outboxProcessor.GetPendingCountAsync();
        return Ok(new { pendingCount = count });
    }

    /// <summary>
    /// Manually process pending outbox messages
    /// </summary>
    [HttpPost("process")]
    public async Task<IActionResult> ProcessPending([FromQuery] int maxMessages = 10)
    {
        var processed = await _outboxProcessor.ProcessPendingMessagesAsync(maxMessages);
        return Ok(new { processedCount = processed });
    }

    /// <summary>
    /// Clean up old processed messages
    /// </summary>
    [HttpDelete("cleanup")]
    public async Task<IActionResult> CleanupOldMessages([FromQuery] int daysToKeep = 7)
    {
        await _outboxProcessor.CleanupOldMessagesAsync(daysToKeep);
        return Ok(new { message = $"Cleaned up messages older than {daysToKeep} days" });
    }
}