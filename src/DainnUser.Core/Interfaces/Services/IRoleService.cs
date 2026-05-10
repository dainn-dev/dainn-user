using DainnUser.Core.Entities;

namespace DainnUser.Core.Interfaces.Services;

/// <summary>
/// Service for managing roles, role permissions, and user role assignments.
/// </summary>
public interface IRoleService
{
    /// <summary>
    /// Creates a new role.
    /// </summary>
    Task<Guid> CreateRoleAsync(
        string name,
        string? description,
        IEnumerable<string> permissions,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing role.
    /// </summary>
    Task<bool> UpdateRoleAsync(
        Guid roleId,
        string name,
        string? description,
        IEnumerable<string> permissions,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an unassigned role.
    /// </summary>
    Task<bool> DeleteRoleAsync(Guid roleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Assigns a role to a user. The operation is idempotent.
    /// </summary>
    Task<bool> AssignRoleToUserAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a role from a user. The operation is idempotent.
    /// </summary>
    Task<bool> RemoveRoleFromUserAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all roles assigned to a user.
    /// </summary>
    Task<IReadOnlyCollection<Role>> GetUserRolesAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets normalized permissions assigned to a role.
    /// </summary>
    Task<IReadOnlyCollection<string>> GetRolePermissionsAsync(Guid roleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all roles.
    /// </summary>
    Task<IReadOnlyCollection<Role>> GetAllRolesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a role by ID.
    /// </summary>
    Task<Role?> GetRoleByIdAsync(Guid roleId, CancellationToken cancellationToken = default);
}
