// using AuthService.Application.Interfaces.Security;

// namespace AuthService.API.Middleware;

// public class JwtBlacklistMiddleware
// {
//     private readonly RequestDelegate _next;

//     public JwtBlacklistMiddleware(RequestDelegate next)
//     {
//         _next = next;
//     }

//     public async Task InvokeAsync(HttpContext context, ITokenBlacklistService tokenBlacklistService)
//     {
//         var authHeader = context.Request.Headers["Authorization"].ToString();
        
//         if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
//         {
//             var token = authHeader.Substring("Bearer ".Length);
            
//             if (await tokenBlacklistService.IsTokenBlacklistedAsync(token))
//             {
//                 context.Response.StatusCode = 401;
//                 await context.Response.WriteAsJsonAsync(new { message = "Token has been revoked. Please login again." });
//                 return;
//             }
//         }

//         await _next(context);
//     }
// }


























using AuthService.Application.Interfaces.Security;

namespace AuthService.API.Middleware;

public class JwtBlacklistMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<JwtBlacklistMiddleware> _logger;

    public JwtBlacklistMiddleware(RequestDelegate next, ILogger<JwtBlacklistMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, ITokenBlacklistService tokenBlacklistService)
    {
        var authHeader = context.Request.Headers["Authorization"].ToString();

        if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
        {
            var token = authHeader.Substring("Bearer ".Length);

            try
            {
                if (await tokenBlacklistService.IsTokenBlacklistedAsync(token))
                {
                    context.Response.StatusCode = 401;
                    await context.Response.WriteAsJsonAsync(new { message = "Token has been revoked. Please login again." });
                    return;
                }
            }
            catch (Exception ex)
            {
                // Redis is down or misconfigured — log it but don't crash the request
                // JWT signature validation still protects the endpoint
                _logger.LogError(ex, "Redis blacklist check failed for token. Allowing request through.");
            }
        }

        await _next(context);
    }
}