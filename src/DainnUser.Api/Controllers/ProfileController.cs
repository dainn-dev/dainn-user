using System.Security.Claims;
using DainnUser.Api.DTOs;
using DainnUser.Api.DTOs.Profile;
using DainnUser.Core.Exceptions;
using DainnUser.Core.Interfaces.Services;
using DainnUser.Core.Models.Profile;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DainnUser.Api.Controllers;

/// <summary>
/// Controller for profile management operations.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProfileController : ControllerBase
{
    private readonly IProfileService _profileService;
    private readonly IAvatarService _avatarService;
    private readonly IValidator<UpdateProfileDto> _updateProfileValidator;
    private readonly ILogger<ProfileController> _logger;

    public ProfileController(
        IProfileService profileService,
        IAvatarService avatarService,
        IValidator<UpdateProfileDto> updateProfileValidator,
        ILogger<ProfileController> logger)
    {
        _profileService = profileService;
        _avatarService = avatarService;
        _updateProfileValidator = updateProfileValidator;
        _logger = logger;
    }

    /// <summary>
    /// Gets the authenticated user's profile.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<ProfileResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ProfileResponse>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<ProfileResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProfile(CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetUserId();
            if (userId == Guid.Empty)
            {
                return Unauthorized(ApiResponse<ProfileResponse>.ErrorResponse("Invalid token."));
            }

            var profile = await _profileService.GetProfileAsync(userId, cancellationToken);
            var response = MapToResponse(profile);

            return Ok(ApiResponse<ProfileResponse>.SuccessResponse(response));
        }
        catch (UserNotFoundException ex)
        {
            _logger.LogWarning(ex, "User not found");
            return NotFound(ApiResponse<ProfileResponse>.ErrorResponse("User not found."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error getting profile");
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                ApiResponse<ProfileResponse>.ErrorResponse("An unexpected error occurred."));
        }
    }

    /// <summary>
    /// Updates the authenticated user's profile.
    /// </summary>
    [HttpPut]
    [ProducesResponseType(typeof(ApiResponse<ProfileResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ProfileResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<ProfileResponse>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<ProfileResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateProfile(
        [FromBody] UpdateProfileDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            var validationResult = await _updateProfileValidator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<ProfileResponse>.ErrorResponse("Validation failed.", errors));
            }

            var userId = GetUserId();
            if (userId == Guid.Empty)
            {
                return Unauthorized(ApiResponse<ProfileResponse>.ErrorResponse("Invalid token."));
            }

            var profile = await _profileService.UpdateProfileAsync(userId, request, cancellationToken);
            var response = MapToResponse(profile);

            return Ok(ApiResponse<ProfileResponse>.SuccessResponse(response, "Profile updated successfully."));
        }
        catch (UserNotFoundException ex)
        {
            _logger.LogWarning(ex, "User not found");
            return NotFound(ApiResponse<ProfileResponse>.ErrorResponse("User not found."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error updating profile");
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                ApiResponse<ProfileResponse>.ErrorResponse("An unexpected error occurred."));
        }
    }

    /// <summary>
    /// Uploads or updates the authenticated user's avatar.
    /// </summary>
    [HttpPost("avatar")]
    [ProducesResponseType(typeof(ApiResponse<ProfileResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ProfileResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<ProfileResponse>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UploadAvatar(
        IFormFile file,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetUserId();
            if (userId == Guid.Empty)
            {
                return Unauthorized(ApiResponse<ProfileResponse>.ErrorResponse("Invalid token."));
            }

            using var stream = file.OpenReadStream();
            var avatarUrl = await _avatarService.UploadAvatarAsync(
                userId,
                file.FileName,
                file.ContentType,
                stream,
                cancellationToken);

            var updateDto = new UpdateProfileDto { AvatarUrl = avatarUrl };
            var updatedProfile = await _profileService.UpdateProfileAsync(userId, updateDto, cancellationToken);
            var response = MapToResponse(updatedProfile);

            return Ok(ApiResponse<ProfileResponse>.SuccessResponse(response, "Avatar uploaded successfully."));
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid avatar upload request");
            return BadRequest(ApiResponse<ProfileResponse>.ErrorResponse(ex.Message));
        }
        catch (UserNotFoundException ex)
        {
            _logger.LogWarning(ex, "User not found");
            return NotFound(ApiResponse<ProfileResponse>.ErrorResponse("User not found."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error uploading avatar");
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                ApiResponse<ProfileResponse>.ErrorResponse("An unexpected error occurred."));
        }
    }

    /// <summary>
    /// Deletes the authenticated user's avatar.
    /// </summary>
    [HttpDelete("avatar")]
    [ProducesResponseType(typeof(ApiResponse<ProfileResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ProfileResponse>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DeleteAvatar(CancellationToken cancellationToken)
    {
        try
        {
            var userId = GetUserId();
            if (userId == Guid.Empty)
            {
                return Unauthorized(ApiResponse<ProfileResponse>.ErrorResponse("Invalid token."));
            }

            var profile = await _profileService.GetProfileAsync(userId, cancellationToken);

            if (!string.IsNullOrWhiteSpace(profile.AvatarUrl))
            {
                await _avatarService.DeleteAvatarAsync(profile.AvatarUrl, cancellationToken);
            }

            var updateDto = new UpdateProfileDto { AvatarUrl = null };
            var updatedProfile = await _profileService.UpdateProfileAsync(userId, updateDto, cancellationToken);
            var response = MapToResponse(updatedProfile);

            return Ok(ApiResponse<ProfileResponse>.SuccessResponse(response, "Avatar deleted successfully."));
        }
        catch (UserNotFoundException ex)
        {
            _logger.LogWarning(ex, "User not found");
            return NotFound(ApiResponse<ProfileResponse>.ErrorResponse("User not found."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error deleting avatar");
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                ApiResponse<ProfileResponse>.ErrorResponse("An unexpected error occurred."));
        }
    }

    /// <summary>
    /// Updates the authenticated user's profile settings (language, timezone).
    /// </summary>
    [HttpPut("settings")]
    [ProducesResponseType(typeof(ApiResponse<ProfileResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ProfileResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<ProfileResponse>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UpdateSettings(
        [FromBody] UpdateProfileDto request,
        CancellationToken cancellationToken)
    {
        // Settings update uses the same DTO and logic as profile update
        return await UpdateProfile(request, cancellationToken);
    }

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                          ?? User.FindFirst("sub")?.Value;

        return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
    }

    private static ProfileResponse MapToResponse(ProfileDto dto)
    {
        return new ProfileResponse
        {
            UserId = dto.UserId,
            Email = dto.Email,
            Username = dto.Username,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            DisplayName = dto.DisplayName,
            AvatarUrl = dto.AvatarUrl,
            DateOfBirth = dto.DateOfBirth,
            Gender = dto.Gender,
            Language = dto.Language,
            Timezone = dto.Timezone,
            Bio = dto.Bio,
            Website = dto.Website,
            CreatedAt = dto.CreatedAt,
            UpdatedAt = dto.UpdatedAt
        };
    }
}
