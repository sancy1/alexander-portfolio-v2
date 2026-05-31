// File: AuthService.API/Controllers/AdminAuthController.cs
// Purpose: Admin authentication endpoints (register, login, logout, profile)
// Layer: API

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MediatR;
using System.Security.Claims;
using AuthService.Application.DTOs.Requests;
using AuthService.Application.DTOs.Responses;
using AuthService.Application.Features.Admin.Commands;
using AuthService.Application.Features.Admin.Queries;
using AuthService.Application.Validators;
using AuthService.Application.Interfaces.Persistence;
using AuthService.Application.Common;
using AuthService.Application.DTOs.Events;
using AuthService.Domain.Enums;

namespace AuthService.API.Controllers;

[ApiController]
[Route("api/v1/admins")]
[Produces("application/json")]
public class AdminAuthController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly AdminRegisterValidator _registerValidator;
    private readonly AdminLoginValidator _loginValidator;
    private readonly IAdminRepository _adminRepository;
    private readonly ISocialUserRepository _socialUserRepository;
    private readonly IOutboxRepository _outboxRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AdminAuthController(
        IMediator mediator,
        AdminRegisterValidator registerValidator,
        AdminLoginValidator loginValidator,
        IAdminRepository adminRepository,
        ISocialUserRepository socialUserRepository,
        IOutboxRepository outboxRepository,
        IUnitOfWork unitOfWork)
    {
        _mediator = mediator;
        _registerValidator = registerValidator;
        _loginValidator = loginValidator;
        _adminRepository = adminRepository;
        _socialUserRepository = socialUserRepository;
        _outboxRepository = outboxRepository;
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Register a new admin account
    /// </summary>
    [HttpPost("register")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Register([FromBody] AdminRegisterRequest request)
    {
        var validationResult = await _registerValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            return BadRequest(new
            {
                errors = validationResult.Errors.Select(e => new { e.PropertyName, e.ErrorMessage })
            });
        }

        var command = new RegisterAdminCommand(
            request.Username,
            request.Email,
            request.Password,
            request.AdminKey
        );

        var result = await _mediator.Send(command);

        if (!result.Success)
        {
            return BadRequest(new { message = result.Message });
        }

        return Ok(new
        {
            token = result.Token,
            adminId = result.UserId,
            message = "Admin registered successfully"
        });
    }

    /// <summary>
    /// Login as an admin
    /// </summary>
    [HttpPost("login")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] AdminLoginRequest request)
    {
        var validationResult = await _loginValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            return BadRequest(new
            {
                errors = validationResult.Errors.Select(e => new { e.PropertyName, e.ErrorMessage })
            });
        }

        var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
        var userAgent = Request.Headers["User-Agent"].ToString() ?? "Unknown";

        var command = new LoginAdminCommand(request.Username, request.Password, clientIp, userAgent);
        var result = await _mediator.Send(command);

        if (!result.Success)
        {
            return Unauthorized(new { message = result.Message });
        }

        return Ok(new
        {
            token = result.Token,
            adminId = result.AdminId,
            username = result.Username,
            email = result.Email,
            message = "Login successful"
        });
    }

    /// <summary>
    /// Logout an admin (blacklists the token in Redis)
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Logout()
    {
        var authHeader = Request.Headers["Authorization"].ToString();
        var token = authHeader.Replace("Bearer ", "");

        if (string.IsNullOrEmpty(token))
        {
            return Unauthorized(new { message = "No token provided" });
        }

        var command = new LogoutAdminCommand(token);
        var result = await _mediator.Send(command);

        if (result)
        {
            return Ok(new { message = "Logout successful" });
        }

        return BadRequest(new { message = "Logout failed" });
    }

    /// <summary>
    /// Get admin profile including extended public metadata fields
    /// </summary>
    [HttpGet("profile")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProfile()
    {
        var adminIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(adminIdClaim))
        {
            return Unauthorized(new { message = "Invalid token" });
        }

        var adminId = Guid.Parse(adminIdClaim);
        var query = new GetAdminProfileQuery(adminId);
        var result = await _mediator.Send(query);

        if (result == null)
        {
            return NotFound(new { message = "Admin not found" });
        }

        return Ok(new
        {
            id = result.Id,
            username = result.Username,
            email = result.Email,
            role = result.Role,
            avatarUrl = result.AvatarUrl,
            status = result.Status,
            createdAt = result.CreatedAt,
            lastLoginAt = result.LastLoginAt,
            updatedAt = result.UpdatedAt,

            // Extended portfolio profile fields
            fullName = result.FullName,
            jobTitle = result.JobTitle,
            headline = result.Headline,
            tagline = result.Tagline,
            bio = result.Bio,
            phone = result.Phone,
            location = result.Location,
            website = result.Website,
            socialLinks = string.IsNullOrEmpty(result.SocialLinks)
                ? null
                : System.Text.Json.JsonSerializer.Deserialize<object>(result.SocialLinks)
        });
    }

    /// <summary>
    /// Update core admin account credentials (username or email)
    /// </summary>
    [HttpPut("profile")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateAdminProfileRequest request)
    {
        var adminIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(adminIdClaim))
        {
            return Unauthorized(new { message = "Invalid token" });
        }

        var adminId = Guid.Parse(adminIdClaim);
        var command = new UpdateAdminProfileCommand(adminId, request.Username, request.Email);
        var result = await _mediator.Send(command);

        if (!result.Success)
        {
            return BadRequest(new { message = result.Message });
        }

        return Ok(new
        {
            message = "Account credentials updated successfully",
            username = result.Username,
            email = result.Email
        });
    }

    /// <summary>
    /// Update extended admin public display profile (bio, social links, showcase details)
    /// </summary>
    [HttpPut("public-profile")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdatePublicProfile([FromBody] UpdatePublicProfileRequest request)
    {
        var adminIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(adminIdClaim))
        {
            return Unauthorized(new { message = "Invalid token" });
        }

        var adminId = Guid.Parse(adminIdClaim);
        var admin = await _adminRepository.GetByIdAsync(adminId);

        if (admin == null)
        {
            return NotFound(new { message = "Admin not found" });
        }

        string? socialLinksJson = null;
        if (request.SocialLinks != null)
        {
            socialLinksJson = System.Text.Json.JsonSerializer.Serialize(
                request.SocialLinks,
                new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
                });
        }

        admin.UpdatePublicProfile(
            request.FullName,
            request.JobTitle,
            request.Headline,
            request.Tagline,
            request.Bio,
            request.Phone,
            request.Location,
            request.Website,
            socialLinksJson
        );

        _adminRepository.Update(admin);

        var profileEvent = new
        {
            eventType = "admin.public_profile_updated",
            adminId = admin.Id,
            username = admin.Username,
            timestamp = DateTime.UtcNow
        };

        await OutboxHelper.AddToOutboxAsync(
            _outboxRepository,
            "admin.profile_updated",
            "admin.profile_updated",
            "rabbitmq",
            profileEvent);

        await OutboxHelper.AddToOutboxAsync(
            _outboxRepository,
            "auth-events",
            "admin.profile_updated",
            "kafka",
            profileEvent);

        await _unitOfWork.SaveChangesAsync();

        return Ok(new
        {
            message = "Public profile updated successfully",
            profile = new
            {
                admin.FullName,
                admin.JobTitle,
                admin.Headline,
                admin.Tagline,
                admin.Bio,
                admin.Phone,
                admin.Location,
                admin.Website,
                SocialLinks = string.IsNullOrEmpty(admin.SocialLinks)
                    ? null
                    : System.Text.Json.JsonSerializer.Deserialize<object>(admin.SocialLinks)
            }
        });
    }

    /// <summary>
    /// Upload admin avatar image
    /// </summary>
    [HttpPost("avatar")]
    [Authorize]
    [ApiExplorerSettings(IgnoreApi = true)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UploadAvatar([FromForm] IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { message = "No file provided" });
        }

        var adminIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(adminIdClaim))
        {
            return Unauthorized(new { message = "Invalid token" });
        }

        var adminId = Guid.Parse(adminIdClaim);
        var command = new UploadAvatarCommand(adminId, file);

        try
        {
            var result = await _mediator.Send(command);

            if (result == null)
            {
                return NotFound(new { message = "Admin not found" });
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

    /// <summary>
    /// Change admin password (requires current password)
    /// </summary>
    [HttpPut("change-password")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var validator = new ChangePasswordValidator();
        var validationResult = await validator.ValidateAsync(request);

        if (!validationResult.IsValid)
        {
            return BadRequest(new
            {
                errors = validationResult.Errors.Select(e => new { e.PropertyName, e.ErrorMessage })
            });
        }

        var adminIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(adminIdClaim))
        {
            return Unauthorized(new { message = "Invalid token" });
        }

        var adminId = Guid.Parse(adminIdClaim);
        var command = new ChangePasswordCommand(adminId, request.CurrentPassword, request.NewPassword);
        var result = await _mediator.Send(command);

        if (!result)
        {
            return BadRequest(new { message = "Current password is incorrect" });
        }

        return Ok(new { message = "Password changed successfully" });
    }

    /// <summary>
    /// Reset forgotten password using Admin Secret Key
    /// </summary>
    [HttpPost("reset-password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        var validator = new ResetPasswordValidator();
        var validationResult = await validator.ValidateAsync(request);

        if (!validationResult.IsValid)
        {
            return BadRequest(new
            {
                errors = validationResult.Errors.Select(e => new { e.PropertyName, e.ErrorMessage })
            });
        }

        var command = new ResetPasswordCommand(request.Username, request.AdminKey, request.NewPassword);
        var result = await _mediator.Send(command);

        if (!result)
        {
            return Unauthorized(new { message = "Invalid username or admin key" });
        }

        return Ok(new { message = "Password reset successfully" });
    }

    /// <summary>
    /// Delete admin account (Soft delete - 30 days reversible OR Hard delete - immediate permanent)
    /// </summary>
    [HttpDelete("account")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DeleteAccount([FromBody] DeleteAccountRequest request)
    {
        if (string.IsNullOrEmpty(request.ConfirmUsername))
        {
            return BadRequest(new { message = "Please enter your username to confirm deletion" });
        }

        var adminIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(adminIdClaim))
        {
            return Unauthorized(new { message = "Invalid token" });
        }

        var adminId = Guid.Parse(adminIdClaim);
        var command = new DeleteAccountCommand(adminId, request.ConfirmUsername, request.PermanentDelete, request.Reason);
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
    /// Restore a soft-deleted account (only works within 30 days)
    /// </summary>
    [HttpPost("account/restore")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RestoreAccount([FromBody] RestoreAccountRequest request)
    {
        if (string.IsNullOrEmpty(request.Username))
        {
            return BadRequest(new { message = "Username is required" });
        }

        if (string.IsNullOrEmpty(request.AdminKey))
        {
            return BadRequest(new { message = "Admin key is required" });
        }

        var command = new RestoreAccountCommand(request.Username, request.AdminKey);
        var result = await _mediator.Send(command);

        if (!result)
        {
            return Unauthorized(new { message = "Cannot restore account. Invalid username, admin key, or account not in soft-deleted state." });
        }

        return Ok(new { message = "Account restored successfully. You can now login again." });
    }

    /// <summary>
    /// Check account deletion status
    /// </summary>
    [HttpGet("account/status")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAccountStatus()
    {
        var adminIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(adminIdClaim))
        {
            return Unauthorized(new { message = "Invalid token" });
        }

        var adminId = Guid.Parse(adminIdClaim);
        var admin = await _adminRepository.GetByIdAsync(adminId);

        if (admin == null)
        {
            return NotFound(new { message = "Admin not found" });
        }

        return Ok(new
        {
            isDeleted = admin.IsDeleted,
            deletedAt = admin.DeletedAt,
            permanentDeleteAt = admin.PermanentDeleteAt,
            canBeRestored = admin.IsDeleted && admin.PermanentDeleteAt > DateTime.UtcNow,
            deleteReason = admin.DeleteReason
        });
    }

    /// <summary>
    /// Admin: Delete a social user account (for violations)
    /// </summary>
    [HttpDelete("admin/social-users/{userId}")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> AdminDeleteSocialUser(Guid userId, [FromBody] AdminDeleteSocialUserRequest request)
    {
        if (string.IsNullOrEmpty(request.Reason))
        {
            return BadRequest(new { message = "Reason for deletion is required" });
        }

        var command = new AdminDeleteSocialUserCommand(userId, request.Reason, request.PermanentDelete);
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
    /// Admin: Block a social user account
    /// </summary>
    [HttpPost("admin/social-users/{userId}/block")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> AdminBlockSocialUser(Guid userId, [FromBody] AdminBlockSocialUserRequest request)
    {
        if (string.IsNullOrEmpty(request.Reason))
        {
            return BadRequest(new { message = "Block reason is required" });
        }

        var command = new AdminBlockSocialUserCommand(userId, request.Reason);
        var result = await _mediator.Send(command);

        if (!result)
        {
            return BadRequest(new { message = "User not found or already blocked" });
        }

        return Ok(new { message = "User has been blocked. User cannot self-restore." });
    }

    /// <summary>
    /// Admin: Unblock a social user account
    /// </summary>
    [HttpPost("admin/social-users/{userId}/unblock")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> AdminUnblockSocialUser(Guid userId)
    {
        var command = new AdminUnblockSocialUserCommand(userId);
        var result = await _mediator.Send(command);

        if (!result)
        {
            return BadRequest(new { message = "User not found or not blocked" });
        }

        return Ok(new { message = "User has been unblocked. User can now self-restore if account is soft-deleted." });
    }

    /// <summary>
    /// Admin: Get all social users with pagination and filters
    /// </summary>
    [HttpGet("admin/social-users")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAllSocialUsers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] string? provider = null,
        [FromQuery] bool? isBlocked = null,
        [FromQuery] bool? isDeleted = null)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        var users = await _socialUserRepository.GetAllAsync(page, pageSize, search, provider, isBlocked, isDeleted);
        var totalCount = await _socialUserRepository.GetTotalCountAsync(search, provider, isBlocked, isDeleted);

        return Ok(new
        {
            users = users.Select(user => new
            {
                id = user.Id,
                email = user.Email,
                displayName = user.DisplayName,
                avatarUrl = user.AvatarUrl,
                provider = user.Provider.ToString(),
                isProfileComplete = user.IsProfileComplete,
                status = user.Status.ToString(),
                isBlocked = user.IsAdminBlocked,
                blockReason = user.AdminBlockReason,
                blockedAt = user.AdminBlockedAt,
                isDeleted = user.IsDeleted,
                createdAt = user.CreatedAt,
                lastLoginAt = user.LastLoginAt
            }),
            pagination = new
            {
                totalCount,
                page,
                pageSize,
                totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
            }
        });
    }

    /// <summary>
    /// Admin: Get single social user by ID
    /// </summary>
    [HttpGet("admin/social-users/{userId}")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSocialUserById(Guid userId)
    {
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
            status = user.Status.ToString(),
            isBlocked = user.IsAdminBlocked,
            blockReason = user.AdminBlockReason,
            blockedAt = user.AdminBlockedAt,
            isDeleted = user.IsDeleted,
            deleteReason = user.DeleteReason,
            deletedAt = user.DeletedAt,
            permanentDeleteAt = user.PermanentDeleteAt,
            createdAt = user.CreatedAt,
            lastLoginAt = user.LastLoginAt
        });
    }

    /// <summary>
    /// Admin: Get blocked social users
    /// </summary>
    [HttpGet("admin/social-users/blocked/list")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetBlockedSocialUsers()
    {
        var users = await _socialUserRepository.GetBlockedUsersAsync();

        return Ok(new
        {
            count = users.Count,
            users = users.Select(user => new
            {
                id = user.Id,
                email = user.Email,
                displayName = user.DisplayName,
                blockReason = user.AdminBlockReason,
                blockedAt = user.AdminBlockedAt
            })
        });
    }

    /// <summary>
    /// Admin: Get recently active social users
    /// </summary>
    [HttpGet("admin/social-users/active/recent")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetRecentlyActiveUsers([FromQuery] int days = 7)
    {
        if (days < 1) days = 1;
        if (days > 90) days = 90;

        var users = await _socialUserRepository.GetRecentlyActiveAsync(days);

        return Ok(new
        {
            days,
            count = users.Count,
            users = users.Select(user => new
            {
                id = user.Id,
                email = user.Email,
                displayName = user.DisplayName,
                provider = user.Provider.ToString(),
                lastLoginAt = user.LastLoginAt
            })
        });
    }

    /// <summary>
    /// Admin: Get social user statistics
    /// </summary>
    [HttpGet("admin/social-users/stats/summary")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetSocialUsersStats()
    {
        var totalCount = await _socialUserRepository.GetTotalCountAsync();
        var blockedCount = await _socialUserRepository.GetTotalCountAsync(isBlocked: true);
        var deletedCount = await _socialUserRepository.GetTotalCountAsync(isDeleted: true);
        var activeCount = await _socialUserRepository.GetTotalCountAsync(isDeleted: false, isBlocked: false);
        var googleCount = await _socialUserRepository.GetTotalCountAsync(provider: "google");
        var githubCount = await _socialUserRepository.GetTotalCountAsync(provider: "github");

        return Ok(new
        {
            totalUsers = totalCount,
            activeUsers = activeCount,
            blockedUsers = blockedCount,
            deletedUsers = deletedCount,
            byProvider = new
            {
                google = googleCount,
                github = githubCount
            },
            lastUpdated = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Admin: Update social user state (activate, deactivate, update details)
    /// </summary>
    [HttpPut("admin/social-users/{userId}")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateSocialUserState(Guid userId, [FromBody] AdminUpdateSocialUserRequest request)
    {
        var user = await _socialUserRepository.GetByIdAsync(userId);

        if (user == null)
        {
            return NotFound(new { message = "User not found" });
        }

        var adminUsername = User.FindFirst(ClaimTypes.Name)?.Value ?? "admin";
        var action = "updated";

        if (!string.IsNullOrEmpty(request.DisplayName) && request.DisplayName != user.DisplayName)
        {
            user.UpdateProfile(request.DisplayName, user.AvatarUrl);
            action = "profile_updated";
        }

        if (request.IsActive.HasValue)
        {
            if (request.IsActive.Value && user.Status != AccountStatus.Active)
            {
                user.Activate();
                action = "activated";
            }
            else if (!request.IsActive.Value && user.Status != AccountStatus.Suspended)
            {
                user.Suspend();
                action = "deactivated";
            }
        }

        _socialUserRepository.Update(user);
        await _unitOfWork.SaveChangesAsync();

        await OutboxHelper.AddToOutboxAsync(
            _outboxRepository,
            _unitOfWork,
            "user.modified",
            "user.modified",
            "rabbitmq",
            new
            {
                UserId = user.Id,
                Email = user.Email,
                DisplayName = user.DisplayName,
                AvatarUrl = user.AvatarUrl,
                Status = user.Status.ToString(),
                IsActive = user.Status == AccountStatus.Active,
                IsBlocked = user.IsAdminBlocked,
                BlockReason = user.AdminBlockReason,
                ModifiedAt = DateTime.UtcNow,
                ModifiedBy = adminUsername,
                Action = action
            });

        return Ok(new
        {
            message = $"User {action} successfully",
            user = new
            {
                id = user.Id,
                email = user.Email,
                displayName = user.DisplayName,
                status = user.Status.ToString(),
                isBlocked = user.IsAdminBlocked
            }
        });
    }
}