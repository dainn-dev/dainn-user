using DainnUser.Core.Entities;

namespace DainnUser.Core.Interfaces.Repositories;

/// <summary>
/// Repository interface for UserSession entity with specific query methods.
/// </summary>
public interface ISessionRepository : IRepository<UserSession>
{
    /// <summary>
    /// Gets a session by its token.
    /// </summary>
    /// <param name="sessionToken">The session token.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The session if found, otherwise null.</returns>
    Task<UserSession?> GetByTokenAsync(string sessionToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets active sessions for a specific user.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of active sessions.</returns>
    Task<IEnumerable<UserSession>> GetActiveByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all sessions for a specific user (active and inactive).
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of all sessions.</returns>
    Task<IEnumerable<UserSession>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets expired sessions.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of expired sessions.</returns>
    Task<IEnumerable<UserSession>> GetExpiredSessionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Deactivates all sessions for a specific user.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DeactivateAllByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deactivates all sessions for a user except the specified session. Used during
    /// change-password to force re-login on other devices while keeping the current session alive.
    /// </summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="keepSessionId">The session to preserve.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DeactivateAllExceptAsync(Guid userId, Guid keepSessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deactivates a specific session.
    /// </summary>
    /// <param name="sessionId">The session identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DeactivateAsync(Guid sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes expired sessions.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of sessions removed.</returns>
    Task<int> RemoveExpiredSessionsAsync(CancellationToken cancellationToken = default);
}
