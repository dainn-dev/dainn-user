using System.Security.Claims;
using DainnUser.Api.DTOs;
using DainnUser.Core.Enums;
using DainnUser.Core.Exceptions;
using DainnUser.Core.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DainnUser.Api.Controllers;

/// <summary>
/// Controller for administrative user management operations.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Administrator")]
public class UserController : ControllerBase
{
    private readonly IUserManagementService _userManagementService;
    private readonly ILogger<UserController> _logger;

    public UserController(
        IUserManagementService userManagementService,
        ILogger<UserController> logger)
    {
        _userManagementService = userManagementService;
        _logger = logger;
    }

    /// <summary>
    /// Gets a paginated list of users.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<UserDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<UserDto>>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<UserDto>>), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetUsers(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] UserStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _userManagementService.GetUsersAsync(pageNumber, pageSize, search, status, cancellationToken);

            var pagedResult = new PagedResult<UserDto>
            {
                Items = result.Items.ToList(),
                TotalCount = result.TotalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            return Ok(ApiResponse<PagedResult<UserDto>>.SuccessResponse(pagedResult));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting users");
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse<PagedResult<UserDto>>.ErrorResponse("An unexpected error occurred."));
        }
    }

    /// <summary>
    /// Gets a user by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetUser(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var user = await _userManagementService.GetUserByIdAsync(id, cancellationToken);
            if (user == null)
            {
                return NotFound(ApiResponse<UserDto>.ErrorResponse("User not found."));
            }

            return Ok(ApiResponse<UserDto>.SuccessResponse(user));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user {UserId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse<UserDto>.ErrorResponse("An unexpected error occurred."));
        }
    }

    /// <summary>
    /// Updates a user.
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UpdateUser(
        Guid id,
        [FromBody] UpdateUserDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            var user = await _userManagementService.UpdateUserAsync(id, request, cancellationToken);
            return Ok(ApiResponse<UserDto>.SuccessResponse(user, "User updated successfully."));
        }
        catch (UserNotFoundException)
        {
            return NotFound(ApiResponse<UserDto>.ErrorResponse("User not found."));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<UserDto>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user {UserId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse<UserDto>.ErrorResponse("An unexpected error occurred."));
        }
    }

    /// <summary>
    /// Deletes a user.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteUser(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var currentUserId = GetUserId();
            if (currentUserId == id)
            {
                return BadRequest(ApiResponse<object>.ErrorResponse("Cannot delete your own account."));
            }

            await _userManagementService.DeleteUserAsync(id, cancellationToken);
            return Ok(ApiResponse<object>.SuccessResponse(null, "User deleted successfully."));
        }
        catch (UserNotFoundException)
        {
            return NotFound(ApiResponse<object>.ErrorResponse("User not found."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user {UserId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse<object>.ErrorResponse("An unexpected error occurred."));
        }
    }

    /// <summary>
    /// Locks a user account.
    /// </summary>
    [HttpPost("{id:guid}/lock")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> LockUser(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var currentUserId = GetUserId();
            if (currentUserId == id)
            {
                return BadRequest(ApiResponse<object>.ErrorResponse("Cannot lock your own account."));
            }

            await _userManagementService.LockUserAsync(id, cancellationToken);
            return Ok(ApiResponse<object>.SuccessResponse(null, "User locked successfully."));
        }
        catch (UserNotFoundException)
        {
            return NotFound(ApiResponse<object>.ErrorResponse("User not found."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error locking user {UserId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse<object>.ErrorResponse("An unexpected error occurred."));
        }
    }

    /// <summary>
    /// Unlocks a user account.
    /// </summary>
    [HttpPost("{id:guid}/unlock")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> UnlockUser(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await _userManagementService.UnlockUserAsync(id, cancellationToken);
            return Ok(ApiResponse<object>.SuccessResponse(null, "User unlocked successfully."));
        }
        catch (UserNotFoundException)
        {
            return NotFound(ApiResponse<object>.ErrorResponse("User not found."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unlocking user {UserId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse<object>.ErrorResponse("An unexpected error occurred."));
        }
    }

    /// <summary>
    /// Adds a role to a user.
    /// </summary>
    [HttpPost("{id:guid}/roles")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> AddRoleToUser(
        Guid id,
        [FromBody] AddRoleRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            await _userManagementService.AddRoleToUserAsync(id, request.RoleId, cancellationToken);
            return Ok(ApiResponse<object>.SuccessResponse(null, "Role added successfully."));
        }
        catch (UserNotFoundException)
        {
            return NotFound(ApiResponse<object>.ErrorResponse("User not found."));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<object>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding role to user {UserId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse<object>.ErrorResponse("An unexpected error occurred."));
        }
    }

    /// <summary>
    /// Removes a role from a user.
    /// </summary>
    [HttpDelete("{id:guid}/roles/{roleId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> RemoveRoleFromUser(
        Guid id,
        Guid roleId,
        CancellationToken cancellationToken)
    {
        try
        {
            await _userManagementService.RemoveRoleFromUserAsync(id, roleId, cancellationToken);
            return Ok(ApiResponse<object>.SuccessResponse(null, "Role removed successfully."));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<object>.ErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing role from user {UserId}", id);
            return StatusCode(StatusCodes.Status500InternalServerError,
                ApiResponse<object>.ErrorResponse("An unexpected error occurred."));
        }
    }

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                          ?? User.FindFirst("sub")?.Value;

        return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
    }
}

/// <summary>
/// Request model for adding a role to a user.
/// </summary>
public class AddRoleRequest
{
    public Guid RoleId { get; set; }
}

/// <summary>
/// Paged result wrapper.
/// </summary>
public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}
