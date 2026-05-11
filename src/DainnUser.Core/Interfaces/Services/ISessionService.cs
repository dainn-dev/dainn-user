using DainnUser.Core.Entities;

namespace DainnUser.Core.Interfaces.Services;

/// <summary>
/// Service interface for user session lifecycle management.
/// </summary>
public interface ISessionService
{
    /// <summary>
    /// Creates a new session for a user with the given refresh token hash and metadata.
    /// </summary>
    Task<UserSession> CreateSessionAsync(
        Guid userId,
        string refreshTokenHash,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active (non-expired, active-flag) sessions for a user.
    /// </summary>
    Task<IReadOnlyCollection<UserSession>> GetActiveSessionsAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes (deactivates) a single session by id. Idempotent when the session
    /// does not exist or is already inactive.
    /// </summary>
    Task RevokeSessionAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes all active sessions for a given user.
    /// </summary>
    Task RevokeAllSessionsAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes all active sessions for a given user except the specified session id.
    /// </summary>
    Task RevokeAllExceptAsync(
        Guid userId,
        Guid keepSessionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Rotates a session's stored token hash and extends its lifetime.
    /// Returns the updated session, or null if the old-hash session was not found,
    /// inactive, or expired.
    /// </summary>
    Task<UserSession?> RotateSessionAsync(
        string oldRefreshTokenHash,
        string newRefreshTokenHash,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the last-activity timestamp of an active session.
    /// Idempotent when the session does not exist or is inactive.
    /// </summary>
    Task UpdateLastActivityAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes all expired sessions from the database and returns the count removed.
    /// </summary>
    Task<int> CleanupExpiredSessionsAsync(
        CancellationToken cancellationToken = default);
}
