# PSA-67 Session Management Design

## Goal

Implement session management service (`ISessionService`) to centralize session lifecycle operations, add max active sessions enforcement, and provide a clean service layer for the authentication flow. Refactor `AuthenticationService` to delegate session operations to the new service.

## Scope

In scope:

- Add `ISessionService` with all session lifecycle methods.
- Add `SessionService` implementation with max sessions enforcement.
- Refactor `AuthenticationService` to use `ISessionService` instead of calling `_unitOfWork.Sessions` directly.
- Session timeout configuration (uses existing `RefreshTokenExpirationDays`).
- Device info tracking via IP address and user agent.
- Last activity timestamp tracking.
- Max active sessions per user enforcement.
- Unit and integration tests.

Out of scope:

- Background cleanup job for expired sessions (deferred to future work).
- API controllers for session management (deferred to PSA-91 admin user management).

## Existing Context

The codebase already has:

- `UserSession` entity with Id, UserId, SessionToken, IpAddress, UserAgent, CreatedAt, ExpiresAt, LastActivityAt, IsActive.
- `ISessionRepository` with all required repository methods.
- `SessionRepository` implementation.
- `DainnUserOptions` with `EnableSessionManagement`, `RefreshTokenExpirationDays`.
- `AuthenticationService` creates sessions directly via `_unitOfWork.Sessions.AddAsync`.

## Service Contract

Create `ISessionService` in `DainnUser.Core.Interfaces.Services`:

```csharp
Task<UserSession> CreateSessionAsync(
    Guid userId,
    string refreshTokenHash,
    string? ipAddress,
    string? userAgent,
    CancellationToken cancellationToken = default);

Task<IReadOnlyCollection<UserSession>> GetActiveSessionsAsync(
    Guid userId,
    CancellationToken cancellationToken = default);

Task RevokeSessionAsync(
    Guid sessionId,
    CancellationToken cancellationToken = default);

Task RevokeAllSessionsAsync(
    Guid userId,
    CancellationToken cancellationToken = default);

Task RevokeAllExceptAsync(
    Guid userId,
    Guid keepSessionId,
    CancellationToken cancellationToken = default);

Task<UserSession?> RotateSessionAsync(
    string oldHash,
    string newHash,
    string? ipAddress,
    string? userAgent,
    CancellationToken cancellationToken = default);

Task UpdateLastActivityAsync(
    Guid sessionId,
    CancellationToken cancellationToken = default);

Task<int> CleanupExpiredSessionsAsync(
    CancellationToken cancellationToken = default);
```

## SessionService Implementation

`SessionService` lives in `DainnUser.Application.Services`, depends on `ISessionRepository` and `IUnitOfWork`.

**Max sessions enforcement:**
- When `CreateSessionAsync` is called and the user already has `MaxActiveSessionsPerUser` active sessions, deactivate the oldest sessions (by `LastActivityAt`) until under the limit before creating the new one.
- `MaxActiveSessionsPerUser` comes from `DainnUserOptions` (default: 5).

**Session expiry:**
- Session expiry is set to `DateTime.UtcNow.AddDays(_options.RefreshTokenExpirationDays)`, matching refresh token lifetime.
- The repository's `GetActiveByUserIdAsync` already filters by `ExpiresAt > DateTime.UtcNow`.

**RotateSessionAsync:**
- Find session by `oldHash`. If found and active, update `SessionToken = newHash`, bump `LastActivityAt = DateTime.UtcNow`, extend `ExpiresAt` to new token expiry.
- If not found, return null (caller falls back to creating a new session).

**CleanupExpiredSessionsAsync:**
- Call `ISessionRepository.RemoveExpiredSessionsAsync` and return the count removed.

## Refactoring AuthenticationService

Remove direct `_unitOfWork.Sessions` calls and replace with `ISessionService`:

- `LoginAsync`: replace session creation block with `ISessionService.CreateSessionAsync`.
- `CompleteTwoFactorLoginAsync`: replace session creation block with `ISessionService.CreateSessionAsync`.
- `RefreshTokenAsync`: replace session rotation logic with `ISessionService.RotateSessionAsync`; if null result, fall back to `CreateSessionAsync`.
- `LogoutAsync`: replace with `ISessionService.RevokeSessionAsync`.
- `ResetPasswordAsync`: replace `DeactivateAllByUserIdAsync` with `ISessionService.RevokeAllSessionsAsync`.
- `ChangePasswordAsync`: replace `DeactivateAllExceptAsync` with `ISessionService.RevokeAllExceptAsync`.

After refactoring, register `ISessionService` in `ApplicationServiceExtensions`.

## Error Handling

- `CreateSessionAsync` for non-existent user: throw `UserNotFoundException`.
- `RevokeSessionAsync` for non-existent session: idempotent (no-op).
- `RotateSessionAsync` when session not found: return null (not an error).
- `GetActiveSessionsAsync` for non-existent user: return empty collection.

## Validation

- No new validation required; session creation uses existing user existence check.
- IP address and user agent are optional; trim if present.

## Testing

Unit tests should cover:

- `CreateSessionAsync` creates session and respects max sessions limit.
- `CreateSessionAsync` throws for unknown user.
- `GetActiveSessionsAsync` returns only active, non-expired sessions.
- `RevokeSessionAsync` marks session inactive.
- `RevokeAllSessionsAsync` deactivates all user sessions.
- `RotateSessionAsync` updates hash and extends expiry when session found.
- `RotateSessionAsync` returns null when session not found.
- `UpdateLastActivityAsync` stamps `LastActivityAt`.

Integration tests should cover:

- Full session lifecycle: create → get active → revoke.
- Max sessions enforcement works end-to-end.
- Session cleanup removes expired rows.

## Security and Compatibility

- No new authorization decisions.
- Session token storage is unchanged (hashed refresh token SHA-256).
- No breaking changes to existing service contracts.
- No in-memory state.
