using DainnUser.Core.Entities;
using DainnUser.Core.Enums;

namespace DainnUser.Core.Interfaces.Services;

/// <summary>
/// Service for administrative user management operations.
/// </summary>
public interface IUserManagementService
{
    /// <summary>
    /// Gets paginated list of all users.
    /// </summary>
    Task<(IEnumerable<UserDto> Items, int TotalCount)> GetUsersAsync(
        int pageNumber = 1,
        int pageSize = 20,
        string? search = null,
        UserStatus? status = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a user by ID.
    /// </summary>
    Task<UserDto?> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a user's information.
    /// </summary>
    Task<UserDto> UpdateUserAsync(Guid userId, UpdateUserDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a user.
    /// </summary>
    Task DeleteUserAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Locks a user account.
    /// </summary>
    Task LockUserAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Unlocks a user account.
    /// </summary>
    Task UnlockUserAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a role to a user.
    /// </summary>
    Task AddRoleToUserAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a role from a user.
    /// </summary>
    Task RemoveRoleFromUserAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Data transfer object for user information in admin views.
/// </summary>
public class UserDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public UserStatus Status { get; set; }
    public bool EmailVerified { get; set; }
    public bool TwoFactorEnabled { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<string> Roles { get; set; } = new();
}

/// <summary>
/// Data transfer object for updating a user.
/// </summary>
public class UpdateUserDto
{
    public string? Email { get; set; }
    public string? Username { get; set; }
    public UserStatus? Status { get; set; }
}
