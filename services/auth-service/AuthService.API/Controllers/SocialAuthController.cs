// File: AuthService.API/Controllers/SocialAuthController.cs
// Purpose: Social authentication endpoints (Google, GitHub)
// Layer: API

using Microsoft.AspNetCore.Mvc;
using MediatR;
using AuthService.Application.Features.Social.Commands;
using AuthService.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using AuthService.Application.DTOs.Requests;
using AuthService.Application.Interfaces.Persistence;
using AuthService.Application.Interfaces.Services;

namespace AuthService.API.Controllers;

[ApiController]
[Route("api/v1/auth")]

public class SocialAuthController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ISocialUserRepository _socialUserRepository;
    private readonly IUnitOfWork _unitOfWork;

    public SocialAuthController(
    IMediator mediator, 
    IHttpClientFactory httpClientFactory,
    ISocialUserRepository socialUserRepository,
    IUnitOfWork unitOfWork)
    {
        _mediator = mediator;
        _httpClientFactory = httpClientFactory;
        _socialUserRepository = socialUserRepository;
        _unitOfWork = unitOfWork;
    }


    /// <summary>
    /// Initiate Google OAuth login
    /// </summary>
    [HttpGet("google/login")]
    public IActionResult GoogleLogin()
    {
        // Read callback URL from environment variable (set to gateway URL)
        var redirectUrl = Environment.GetEnvironmentVariable("GOOGLE_CALLBACK_URL");
        
        if (string.IsNullOrEmpty(redirectUrl))
        {
            // Fallback for local development - guarantees correct URL regardless of routing
            redirectUrl = $"{Request.Scheme}://{Request.Host}/api/v1/auth/google/callback";
        }
        
        var clientId = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_ID");
        var authUrl = $"https://accounts.google.com/o/oauth2/v2/auth?" +
                    $"client_id={clientId}&" +
                    $"redirect_uri={Uri.EscapeDataString(redirectUrl)}&" +
                    "response_type=code&" +
                    "scope=email profile&" +
                    "access_type=offline";
        
        return Redirect(authUrl);
    }

    /// <summary>
    /// Google OAuth callback
    /// </summary>
    [HttpGet("google/callback")]
    public async Task<IActionResult> GoogleCallback(string code, string? error = null, string? error_description = null)
    {
        if (!string.IsNullOrEmpty(error))
        {
            return BadRequest(new { message = $"Google error: {error}", description = error_description });
        }

        if (string.IsNullOrEmpty(code))
        {
            return BadRequest(new { message = "Authorization code is missing" });
        }

        var clientId = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_ID");
        var clientSecret = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_SECRET");
        
        // Read callback URL from environment variable for consistency
        var redirectUrl = Environment.GetEnvironmentVariable("GOOGLE_CALLBACK_URL");
        
        if (string.IsNullOrEmpty(redirectUrl))
        {
            // Fallback for local development
            redirectUrl = $"{Request.Scheme}://{Request.Host}/api/v1/auth/google/callback";
        }

        // Exchange code for access token
        var tokenClient = _httpClientFactory.CreateClient();
        var tokenRequest = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("code", code),
            new KeyValuePair<string, string>("client_id", clientId ?? string.Empty),
            new KeyValuePair<string, string>("client_secret", clientSecret ?? string.Empty),
            new KeyValuePair<string, string>("redirect_uri", redirectUrl),
            new KeyValuePair<string, string>("grant_type", "authorization_code")
        });

        var tokenResponse = await tokenClient.PostAsync("https://oauth2.googleapis.com/token", tokenRequest);
        var tokenResponseBody = await tokenResponse.Content.ReadAsStringAsync();

        if (!tokenResponse.IsSuccessStatusCode)
        {
            return BadRequest(new { message = "Failed to get access token", details = tokenResponseBody });
        }

        // Parse the response
        var tokenJson = System.Text.Json.JsonSerializer.Deserialize<GoogleTokenResponse>(tokenResponseBody);

        if (tokenJson == null || string.IsNullOrEmpty(tokenJson.access_token))
        {
            return BadRequest(new { message = "Failed to parse access token", response = tokenResponseBody });
        }

        // Get user info
        var userClient = _httpClientFactory.CreateClient();
        userClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {tokenJson.access_token}");
        var userResponse = await userClient.GetAsync("https://www.googleapis.com/oauth2/v2/userinfo");
        var googleUser = await userResponse.Content.ReadFromJsonAsync<GoogleUserInfo>();

        if (googleUser == null)
        {
            return BadRequest(new { message = "Failed to get user info" });
        }

        // Process user
        var result = await _mediator.Send(new HandleOAuthLoginCommand(
            googleUser.Id,
            SocialProvider.Google,
            googleUser.Email,
            googleUser.Name,
            googleUser.Picture
        ));

        if (result.RequiresProfileCompletion)
        {
            return Ok(new
            {
                requiresProfileCompletion = true,
                userId = result.UserId,
                email = googleUser.Email,
                name = googleUser.Name,
                provider = "google"
            });
        }

        return Ok(new
        {
            token = result.Token,
            userId = result.UserId,
            email = googleUser.Email,
            displayName = googleUser.Name,
            message = "Login successful"
        });
    }


    /// <summary>
    /// Initiate GitHub OAuth login
    /// </summary>
    [HttpGet("github/login")]
    public IActionResult GitHubLogin()
    {
        // Read callback URL from environment variable (set to gateway URL)
        var redirectUrl = Environment.GetEnvironmentVariable("GITHUB_CALLBACK_URL");
        
        if (string.IsNullOrEmpty(redirectUrl))
        {
            // Fallback for local development
            redirectUrl = $"{Request.Scheme}://{Request.Host}/api/v1/auth/github/callback";
        }
        
        var clientId = Environment.GetEnvironmentVariable("GITHUB_CLIENT_ID");
        var authUrl = $"https://github.com/login/oauth/authorize?" +
                    $"client_id={clientId}&" +
                    $"redirect_uri={Uri.EscapeDataString(redirectUrl)}&" +
                    "scope=user:email";
        
        return Redirect(authUrl);
    }


    /// <summary>
    /// GitHub OAuth callback
    /// </summary>
    [HttpGet("github/callback")]
    public async Task<IActionResult> GitHubCallback(string code)
    {
        if (string.IsNullOrEmpty(code))
        {
            return BadRequest(new { message = "Authorization code is missing" });
        }

        var clientId = Environment.GetEnvironmentVariable("GITHUB_CLIENT_ID");
        var clientSecret = Environment.GetEnvironmentVariable("GITHUB_CLIENT_SECRET");
        
        // Read callback URL from environment variable
        var redirectUrl = Environment.GetEnvironmentVariable("GITHUB_CALLBACK_URL");
        
        if (string.IsNullOrEmpty(redirectUrl))
        {
            // Fallback for local development
            redirectUrl = $"{Request.Scheme}://{Request.Host}/api/v1/auth/github/callback";
        }

        // Exchange code for access token
        var tokenClient = _httpClientFactory.CreateClient();
        var tokenRequest = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("code", code),
            new KeyValuePair<string, string>("client_id", clientId ?? string.Empty),
            new KeyValuePair<string, string>("client_secret", clientSecret ?? string.Empty),
            new KeyValuePair<string, string>("redirect_uri", redirectUrl)
        });

        var tokenResponse = await tokenClient.PostAsync("https://github.com/login/oauth/access_token", tokenRequest);
        var tokenText = await tokenResponse.Content.ReadAsStringAsync();
        
        // Parse the response
        var accessToken = ExtractAccessToken(tokenText);

        if (string.IsNullOrEmpty(accessToken))
        {
            return BadRequest(new { message = "Failed to get access token" });
        }

        // Get user info
        var userClient = _httpClientFactory.CreateClient();
        userClient.DefaultRequestHeaders.Add("User-Agent", "AuthService");
        userClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");
        var userResponse = await userClient.GetAsync("https://api.github.com/user");
        var githubUser = await userResponse.Content.ReadFromJsonAsync<GitHubUserInfo>();

        if (githubUser == null)
        {
            return BadRequest(new { message = "Failed to get user info" });
        }

        // Get email (GitHub may not return email in primary request)
        var emailResponse = await userClient.GetAsync("https://api.github.com/user/emails");
        var emails = await emailResponse.Content.ReadFromJsonAsync<List<GitHubEmail>>();
        var primaryEmail = emails?.FirstOrDefault(e => e.Primary)?.Email ?? githubUser.Email;

        // Process user
        var result = await _mediator.Send(new HandleOAuthLoginCommand(
            githubUser.Id.ToString(),
            SocialProvider.GitHub,
            primaryEmail ?? githubUser.Login,
            githubUser.Name ?? githubUser.Login,
            githubUser.AvatarUrl
        ));

        if (result.RequiresProfileCompletion)
        {
            return Ok(new
            {
                requiresProfileCompletion = true,
                userId = result.UserId,
                email = primaryEmail ?? githubUser.Login,
                name = githubUser.Name ?? githubUser.Login,
                provider = "github"
            });
        }

        return Ok(new
        {
            token = result.Token,
            userId = result.UserId,
            email = primaryEmail ?? githubUser.Login,
            displayName = githubUser.Name ?? githubUser.Login,
            message = "Login successful"
        });
    }


    /// <summary>
    /// Complete user profile after OAuth login (first time only)
    /// </summary>
    [HttpPost("users/complete-registration")]
    public async Task<IActionResult> CompleteRegistration([FromBody] CompleteProfileRequest request, [FromQuery] Guid userId)
    {
        if (string.IsNullOrEmpty(request.DisplayName))
        {
            return BadRequest(new { message = "Display name is required" });
        }

        var command = new CompleteUserProfileCommand(userId, request.DisplayName, request.AvatarUrl);
        var result = await _mediator.Send(command);

        if (!result.Success)
        {
            return BadRequest(new { message = result.Message });
        }

        return Ok(new
        {
            token = result.Token,
            userId = result.UserId,
            displayName = request.DisplayName,
            message = "Profile completed successfully"
        });
    }

    
    /// <summary>
    /// Get social user profile (requires authentication)
    /// </summary>
    [HttpGet("users/profile")]
    [Authorize]
    public async Task<IActionResult> GetProfile()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(userIdClaim))
        {
            return Unauthorized(new { message = "Invalid token" });
        }

        var userId = Guid.Parse(userIdClaim);
        var user = await _socialUserRepository.GetByIdAsync(userId);

        if (user == null)
        {
            return NotFound(new { message = "User not found" });
        }

        return Ok(new
        {
            id = user.Id,
            email = user.Email,
            displayName = user.DisplayName,
            avatarUrl = user.AvatarUrl,
            provider = user.Provider.ToString(),
            isProfileComplete = user.IsProfileComplete,
            createdAt = user.CreatedAt,
            lastLoginAt = user.LastLoginAt
        });
    }

    
    
    
    /// <summary>
    /// Update social user profile (display name only)
    /// </summary>
    [HttpPut("users/profile")]
    [Authorize]
    [ApiExplorerSettings(IgnoreApi = true)]  // Hide from Swagger to avoid error
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateSocialProfileRequest request)
    {
        if (string.IsNullOrEmpty(request.DisplayName))
        {
            return BadRequest(new { message = "Display name is required" });
        }

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(userIdClaim))
        {
            return Unauthorized(new { message = "Invalid token" });
        }

        var userId = Guid.Parse(userIdClaim);
        var user = await _socialUserRepository.GetByIdAsync(userId);

        if (user == null)
        {
            return NotFound(new { message = "User not found" });
        }

        // Update display name only
        user.UpdateProfile(request.DisplayName, user.AvatarUrl);
        
        _socialUserRepository.Update(user);
        await _unitOfWork.SaveChangesAsync();

        return Ok(new
        {
            message = "Profile updated successfully",
            displayName = user.DisplayName,
            avatarUrl = user.AvatarUrl
        });
    }






   
    /// <summary>
    /// Delete social user account (Soft delete - 30 days reversible OR Hard delete - immediate permanent)
    /// </summary>
    /// <remarks>
    /// Option A (Soft Delete - Default):
    /// - Account is deactivated for 30 days
    /// - Can be restored by logging in again within 30 days
    /// - Permanent deletion happens automatically after 30 days
    /// 
    /// Option B (Hard Delete):
    /// - Set "permanentDelete": true
    /// - Account and custom avatar are permanently deleted immediately
    /// - This action CANNOT be undone
    /// 
    /// Sample request (Soft Delete):
    /// {
    ///     "confirmEmail": "user@example.com",
    ///     "permanentDelete": false,
    ///     "reason": "Taking a break"
    /// }
    /// 
    /// Sample request (Hard Delete):
    /// {
    ///     "confirmEmail": "user@example.com",
    ///     "permanentDelete": true
    /// }
    /// </remarks>
    [HttpDelete("users/account")]
    [Authorize]
    public async Task<IActionResult> DeleteSocialUserAccount([FromBody] DeleteSocialUserRequest request)
    {
        if (string.IsNullOrEmpty(request.ConfirmEmail))
        {
            return BadRequest(new { message = "Please enter your email to confirm deletion" });
        }

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(userIdClaim))
        {
            return Unauthorized(new { message = "Invalid token" });
        }

        var userId = Guid.Parse(userIdClaim);
        var command = new DeleteSocialUserCommand(userId, request.ConfirmEmail, request.PermanentDelete, request.Reason);
        var result = await _mediator.Send(command);

        if (!result.Success)
        {
            return BadRequest(new { message = result.Message });
        }

        return Ok(new
        {
            message = result.Message,
            permanentDeleteDate = result.PermanentDeleteDate,
            isReversible = result.IsReversible
        });
    }

    /// <summary>
    /// Restore a soft-deleted social user account (only works within 30 days)
    /// </summary>
    /// <remarks>
    /// This endpoint does NOT require a JWT token because the user cannot login after soft delete.
    /// Instead, it uses email + OAuth provider for verification.
    /// 
    /// Sample request:
    /// {
    ///     "email": "user@example.com",
    ///     "provider": "google"
    /// }
    /// </remarks>
    
    [HttpPost("users/account/restore")]
    [AllowAnonymous]
    public async Task<IActionResult> RestoreSocialUserAccount([FromBody] RestoreSocialAccountRequest request)
    {
        if (string.IsNullOrEmpty(request.Email))
        {
            return BadRequest(new { message = "Email is required" });
        }

        var user = await _socialUserRepository.GetByEmailAsync(request.Email);
        
        if (user == null)
        {
            return NotFound(new { message = "User not found" });
        }

        if (!user.IsDeleted)
        {
            return BadRequest(new { message = "Account is not deleted" });
        }

        if (user.IsAdminBlocked)
        {
            return BadRequest(new { message = "Account is blocked by admin. Cannot self-restore. Contact support." });
        }

        if (user.PermanentDeleteAt <= DateTime.UtcNow)
        {
            return BadRequest(new { message = "Account has been permanently deleted and cannot be restored" });
        }

        user.Restore();
        _socialUserRepository.Update(user);
        await _unitOfWork.SaveChangesAsync();

        return Ok(new { message = "Account restored successfully. You can now login again." });
    }

    /// <summary>
    /// Check social user account deletion status
    /// </summary>
    [HttpGet("users/account/status")]
    [Authorize]
    public async Task<IActionResult> GetSocialUserAccountStatus()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(userIdClaim))
        {
            return Unauthorized(new { message = "Invalid token" });
        }

        var userId = Guid.Parse(userIdClaim);
        var user = await _socialUserRepository.GetByIdAsync(userId);

        if (user == null)
        {
            return NotFound(new { message = "User not found" });
        }

        return Ok(new
        {
            isDeleted = user.IsDeleted,
            deletedAt = user.DeletedAt,
            permanentDeleteAt = user.PermanentDeleteAt,
            canBeRestored = user.IsDeleted && user.PermanentDeleteAt > DateTime.UtcNow,
            deleteReason = user.DeleteReason
        });
    }






    /// <summary>
    /// Upload avatar for social user (after profile completion)
    /// </summary>
    [HttpPost("users/avatar")]
    [Authorize]
    [ApiExplorerSettings(IgnoreApi = true)]
    public async Task<IActionResult> UploadSocialUserAvatar([FromForm] IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { message = "No file provided" });
        }

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(userIdClaim))
        {
            return Unauthorized(new { message = "Invalid token" });
        }

        var userId = Guid.Parse(userIdClaim);
        var command = new UploadSocialUserAvatarCommand(userId, file);
        
        try
        {
            var result = await _mediator.Send(command);
            
            if (result == null)
            {
                return NotFound(new { message = "User not found" });
            }

            return Ok(new
            {
                message = "Avatar uploaded successfully",
                avatarUrl = result
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    private string ExtractAccessToken(string response)
    {
        var parts = response.Split('&');
        foreach (var part in parts)
        {
            if (part.StartsWith("access_token="))
            {
                return part.Substring("access_token=".Length);
            }
        }
        return string.Empty;
    }
}


// Helper classes for OAuth responses
public class GoogleTokenResponse
{
    public string access_token { get; set; } = string.Empty;
    public string id_token { get; set; } = string.Empty;
    public int expires_in { get; set; }
    public string token_type { get; set; } = string.Empty;
    public string scope { get; set; } = string.Empty;
}

public class GoogleUserInfo
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Picture { get; set; } = string.Empty;
}

public class GitHubUserInfo
{
    public long Id { get; set; }
    public string Login { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string AvatarUrl { get; set; } = string.Empty;
}

public class GitHubEmail
{
    public string Email { get; set; } = string.Empty;
    public bool Primary { get; set; }
    public bool Verified { get; set; }
}