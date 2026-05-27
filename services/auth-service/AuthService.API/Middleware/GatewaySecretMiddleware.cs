// File: AuthService.API/Middleware/GatewaySecretMiddleware.cs
// Purpose: Blocks any request that didn't come through the API gateway
// Layer: API

namespace AuthService.API.Middleware;

public class GatewaySecretMiddleware
{
    private readonly RequestDelegate _next;
    private readonly string _gatewaySecret;
    private readonly ILogger<GatewaySecretMiddleware> _logger;

    // These paths bypass the check entirely (health checks need to work
    // for Render's own infrastructure monitoring)
    private static readonly string[] BypassPaths =
    [
        "/health/ready",
        "/health/live",
        "/health"
    ];

    public GatewaySecretMiddleware(
        RequestDelegate next,
        IConfiguration configuration,
        ILogger<GatewaySecretMiddleware> logger)
    {
        _next = next;
        _logger = logger;
        _gatewaySecret = Environment.GetEnvironmentVariable("GATEWAY_SECRET")
            ?? configuration["GATEWAY_SECRET"]
            ?? string.Empty;

        if (string.IsNullOrEmpty(_gatewaySecret))
        {
            _logger.LogWarning(
                "GATEWAY_SECRET is not configured. " +
                "All requests will be allowed through. " +
                "Set GATEWAY_SECRET in production!");
        }
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip check if secret not configured (dev environment safety net)
        if (string.IsNullOrEmpty(_gatewaySecret))
        {
            await _next(context);
            return;
        }

        // Skip check for health check paths (Render needs these)
        var path = context.Request.Path.Value ?? string.Empty;
        if (BypassPaths.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
        {
            await _next(context);
            return;
        }

        // Check for gateway secret header
        var providedSecret = context.Request.Headers["X-Gateway-Secret"].FirstOrDefault();

        if (string.IsNullOrEmpty(providedSecret) ||
            !string.Equals(providedSecret, _gatewaySecret, StringComparison.Ordinal))
        {
            _logger.LogWarning(
                "Blocked direct access attempt to {Path} from {IP}",
                path,
                context.Connection.RemoteIpAddress);

            context.Response.StatusCode = 403;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(
                """{"error":"Access denied. Use the API gateway."}""");
            return;
        }

        await _next(context);
    }
}