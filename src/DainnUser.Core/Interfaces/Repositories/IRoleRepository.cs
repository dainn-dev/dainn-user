using DainnUser.Core.Entities;

namespace DainnUser.Core.Interfaces.Repositories;

/// <summary>
/// Repository interface for Role entity with specific query methods.
/// </summary>
public interface IRoleRepository : IRepository<Role>
{
    /// <summary>
    /// Gets a role by name.
    /// </summary>
    /// <param name="name">The role name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The role if found, otherwise null.</returns>
    Task<Role?> GetByNameAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets roles by user ID.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of roles assigned to the user.</returns>
    Task<IEnumerable<Role>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a role with its assigned users.
    /// </summary>
    /// <param name="roleId">The role identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The role with users if found, otherwise null.</returns>
    Task<Role?> GetWithUsersAsync(Guid roleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a role name is already taken.
    /// </summary>
    /// <param name="name">The role name.</param>
    /// <param name="excludeRoleId">Optional role ID to exclude from check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if role name is taken, otherwise false.</returns>
    Task<bool> IsNameTakenAsync(string name, Guid? excludeRoleId = null, CancellationToken cancellationToken = default);
}
