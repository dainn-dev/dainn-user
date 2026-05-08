using DainnUser.Core.Entities;
using DainnUser.Core.Enums;

namespace DainnUser.Core.Interfaces.Repositories;

/// <summary>
/// Repository interface for User entity with specific query methods.
/// </summary>
public interface IUserRepository : IRepository<User>
{
    /// <summary>
    /// Gets a user by email address.
    /// </summary>
    /// <param name="email">The email address.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The user if found, otherwise null.</returns>
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a user by email address with tokens.
    /// </summary>
    /// <param name="email">The email address.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The user with tokens if found, otherwise null.</returns>
    Task<User?> GetByEmailWithTokensAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a user by ID with tokens.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The user with tokens if found, otherwise null.</returns>
    Task<User?> GetByIdWithTokensAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a user by username.
    /// </summary>
    /// <param name="username">The username.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The user if found, otherwise null.</returns>
    Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a user with their profile information.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The user with profile if found, otherwise null.</returns>
    Task<User?> GetWithProfileAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a user with their roles.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The user with roles if found, otherwise null.</returns>
    Task<User?> GetWithRolesAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets users by status.
    /// </summary>
    /// <param name="status">The user status.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of users with the specified status.</returns>
    Task<IEnumerable<User>> GetByStatusAsync(UserStatus status, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a user by external login provider.
    /// </summary>
    /// <param name="provider">The login provider.</param>
    /// <param name="providerKey">The provider-specific key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The user if found, otherwise null.</returns>
    Task<User?> GetByExternalLoginAsync(LoginProvider provider, string providerKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if an email is already taken.
    /// </summary>
    /// <param name="email">The email address.</param>
    /// <param name="excludeUserId">Optional user ID to exclude from check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if email is taken, otherwise false.</returns>
    Task<bool> IsEmailTakenAsync(string email, Guid? excludeUserId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a username is already taken.
    /// </summary>
    /// <param name="username">The username.</param>
    /// <param name="excludeUserId">Optional user ID to exclude from check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if username is taken, otherwise false.</returns>
    Task<bool> IsUsernameTakenAsync(string username, Guid? excludeUserId = null, CancellationToken cancellationToken = default);
}
