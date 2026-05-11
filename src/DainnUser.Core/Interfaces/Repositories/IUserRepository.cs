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
    /// Gets a user by email address with their roles loaded.
    /// </summary>
    /// <param name="email">The email address.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The user with roles if found, otherwise null.</returns>
    Task<User?> GetByEmailWithRolesAsync(string email, CancellationToken cancellationToken = default);

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
    /// Adds a <see cref="UserToken"/> to the context. Prefer this over mutating
    /// <see cref="User.Tokens"/> on a tracked user, which the EF Core InMemory provider
    /// handles inconsistently.
    /// </summary>
    /// <param name="token">The token to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task AddTokenAsync(UserToken token, CancellationToken cancellationToken = default);

    /// <summary>
    /// Looks up a password-reset token row by its stored hash, regardless of state.
    /// Callers must check <c>IsUsed</c>/<c>IsRevoked</c>/<c>ExpiresAt</c>.
    /// </summary>
    /// <param name="tokenHash">SHA-256 hash of the plain reset token.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The token with its <see cref="UserToken.User"/> navigation populated, or null.</returns>
    Task<UserToken?> GetPasswordResetTokenByHashAsync(string tokenHash, CancellationToken cancellationToken = default);

    /// <summary>
    /// Looks up a refresh token row by its stored hash, regardless of state (used/revoked/expired).
    /// Callers must check <c>IsUsed</c>/<c>IsRevoked</c>/<c>ExpiresAt</c> themselves.
    /// </summary>
    /// <param name="tokenHash">SHA-256 hash of the plain refresh token.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The matching token, or null if no token has that hash.</returns>
    Task<UserToken?> GetRefreshTokenByHashAsync(string tokenHash, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes every active refresh token for a user. Used for token-reuse incident response —
    /// callers should also deactivate sessions via <see cref="ISessionRepository.DeactivateAllByUserIdAsync"/>.
    /// </summary>
    /// <param name="userId">The user whose refresh tokens should be revoked.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RevokeAllRefreshTokensAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes every active refresh token for a user except the token tied to the specified session.
    /// Used during change-password to force re-login on all other devices while keeping the current
    /// session alive.
    /// </summary>
    /// <param name="userId">The user whose other refresh tokens should be revoked.</param>
    /// <param name="keepSessionId">The session whose refresh token should be preserved.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RevokeAllRefreshTokensExceptSessionAsync(Guid userId, Guid keepSessionId, CancellationToken cancellationToken = default);

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

    /// <summary>
    /// Adds a user profile to the database. Intended for cases where the user entity
    /// is already tracked and <c>User.Profile</c> should not be set directly.
    /// </summary>
    /// <param name="profile">The profile to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task AddProfileAsync(UserProfile profile, CancellationToken cancellationToken = default);
}
