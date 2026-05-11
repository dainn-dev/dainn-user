# PSA-67 Session Management Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add `ISessionService` / `SessionService` to centralize session lifecycle, enforce max sessions, and refactor `AuthenticationService` to delegate session operations.

**Architecture:** SessionService depends on ISessionRepository + DainnUserOptions. AuthenticationService depends on ISessionService instead of calling IUnitOfWork.Sessions directly. Follows existing service/interface/DI patterns exactly like RoleService and ProfileService.

**Tech Stack:** .NET 8, xUnit, Moq, FluentAssertions

---

## File Map

```
src/DainnUser.Core/Interfaces/Services/
  + ISessionService.cs              ← new

src/DainnUser.Application/Services/
  + SessionService.cs               ← new

src/DainnUser.Application/
  ApplicationServiceExtensions.cs   ← modify (add DI registration)

src/DainnUser.Core/Interfaces/Services/IAuthenticationService.cs
                                    ← modify (remove session-specific concerns)

src/DainnUser.Application/Services/AuthenticationService.cs
                                    ← modify (delegate session ops to ISessionService)

src/DainnUser.Infrastructure/Configuration/DainnUserOptions.cs
                                    ← modify (add MaxActiveSessionsPerUser)

tests/DainnUser.UnitTests/Services/
  + SessionServiceTests.cs          ← new

tests/DainnUser.IntegrationTests/Services/
  + SessionServiceIntegrationTests.cs ← new
```

---

## Task 1: Add MaxActiveSessionsPerUser to DainnUserOptions

**Files:**
- Modify: `src/DainnUser.Infrastructure/Configuration/DainnUserOptions.cs`

Add after the `RefreshTokenExpirationDays` property:

```csharp
/// <summary>
/// Gets or sets the maximum number of active sessions allowed per user.
/// When exceeded, the oldest session is deactivated.
/// </summary>
public int MaxActiveSessionsPerUser { get; set; } = 5;
```

---

## Task 2: Create ISessionService interface

**Files:**
- Create: `src/DainnUser.Core/Interfaces/Services/ISessionService.cs`

```csharp
using DainnUser.Core.Entities;

namespace DainnUser.Core.Interfaces.Services;

public interface ISessionService
{
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
        string oldRefreshTokenHash,
        string newRefreshTokenHash,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default);

    Task UpdateLastActivityAsync(
        Guid sessionId,
        CancellationToken cancellationToken = default);

    Task<int> CleanupExpiredSessionsAsync(
        CancellationToken cancellationToken = default);
}
```

---

## Task 3: Create SessionService implementation

**Files:**
- Create: `src/DainnUser.Application/Services/SessionService.cs`

```csharp
using DainnUser.Core.Entities;
using DainnUser.Core.Exceptions;
using DainnUser.Core.Interfaces.Repositories;
using DainnUser.Core.Interfaces.Services;
using DainnUser.Infrastructure.Configuration;
using Microsoft.Extensions.Options;

namespace DainnUser.Application.Services;

public class SessionService : ISessionService
{
    private readonly ISessionRepository _sessionRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly DainnUserOptions _options;

    public SessionService(
        ISessionRepository sessionRepository,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        IOptions<DainnUserOptions> options)
    {
        _sessionRepository = sessionRepository;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _options = options.Value;
    }

    public async Task<UserSession> CreateSessionAsync(
        Guid userId,
        string refreshTokenHash,
        string? ipAddress,
        string? userAgent,
        CancellationToken ct = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, ct);
        if (user is null)
            throw new UserNotFoundException(userId);

        // Enforce max active sessions per user
        if (_options.MaxActiveSessionsPerUser > 0)
        {
            var activeSessions = (await _sessionRepository.GetActiveByUserIdAsync(userId, ct))
                .OrderBy(s => s.LastActivityAt)
                .ToList();

            // Deactivate oldest sessions until we're under the limit (minus 1 for new session)
            while (activeSessions.Count >= _options.MaxActiveSessionsPerUser)
            {
                var oldest = activeSessions[0];
                oldest.IsActive = false;
                oldest.LastActivityAt = DateTime.UtcNow;
                activeSessions.RemoveAt(0);
            }
        }

        var expiresAt = DateTime.UtcNow.AddDays(_options.RefreshTokenExpirationDays);
        var session = new UserSession
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            SessionToken = refreshTokenHash,
            IpAddress = Normalize(ipAddress),
            UserAgent = Normalize(userAgent),
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = expiresAt,
            LastActivityAt = DateTime.UtcNow,
            IsActive = true
        };

        await _sessionRepository.AddAsync(session, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return session;
    }

    public async Task<IReadOnlyCollection<UserSession>> GetActiveSessionsAsync(
        Guid userId, CancellationToken ct = default)
    {
        var sessions = await _sessionRepository.GetActiveByUserIdAsync(userId, ct);
        return sessions.ToList();
    }

    public async Task RevokeSessionAsync(Guid sessionId, CancellationToken ct = default)
    {
        var session = await _sessionRepository.GetByIdAsync(sessionId, ct);
        if (session is not null && session.IsActive)
        {
            session.IsActive = false;
            session.LastActivityAt = DateTime.UtcNow;
            await _unitOfWork.SaveChangesAsync(ct);
        }
    }

    public async Task RevokeAllSessionsAsync(Guid userId, CancellationToken ct = default)
    {
        var sessions = await _sessionRepository.GetActiveByUserIdAsync(userId, ct);
        foreach (var s in sessions)
        {
            s.IsActive = false;
            s.LastActivityAt = DateTime.UtcNow;
        }

        await _unitOfWork.SaveChangesAsync(ct);
    }

    public async Task RevokeAllExceptAsync(
        Guid userId, Guid keepSessionId, CancellationToken ct = default)
    {
        var sessions = await _sessionRepository.GetActiveByUserIdAsync(userId, ct);
        foreach (var s in sessions.Where(s => s.Id != keepSessionId))
        {
            s.IsActive = false;
            s.LastActivityAt = DateTime.UtcNow;
        }

        await _unitOfWork.SaveChangesAsync(ct);
    }

    public async Task<UserSession?> RotateSessionAsync(
        string oldRefreshTokenHash,
        string newRefreshTokenHash,
        string? ipAddress,
        string? userAgent,
        CancellationToken ct = default)
    {
        var session = await _sessionRepository.GetByTokenAsync(oldRefreshTokenHash, ct);
        if (session is null || !session.IsActive)
            return null;

        if (session.ExpiresAt <= DateTime.UtcNow)
        {
            session.IsActive = false;
            await _unitOfWork.SaveChangesAsync(ct);
            return null;
        }

        session.SessionToken = newRefreshTokenHash;
        session.LastActivityAt = DateTime.UtcNow;
        session.ExpiresAt = DateTime.UtcNow.AddDays(_options.RefreshTokenExpirationDays);
        if (!string.IsNullOrWhiteSpace(ipAddress))
            session.IpAddress = Normalize(ipAddress);
        if (!string.IsNullOrWhiteSpace(userAgent))
            session.UserAgent = Normalize(userAgent);

        await _unitOfWork.SaveChangesAsync(ct);
        return session;
    }

    public async Task UpdateLastActivityAsync(Guid sessionId, CancellationToken ct = default)
    {
        var session = await _sessionRepository.GetByIdAsync(sessionId, ct);
        if (session is not null && session.IsActive)
        {
            session.LastActivityAt = DateTime.UtcNow;
            await _unitOfWork.SaveChangesAsync(ct);
        }
    }

    public async Task<int> CleanupExpiredSessionsAsync(CancellationToken ct = default)
    {
        var count = await _sessionRepository.RemoveExpiredSessionsAsync(ct);
        if (count > 0)
            await _unitOfWork.SaveChangesAsync(ct);

        return count;
    }

    private static string? Normalize(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
```

---

## Task 4: Register SessionService in DI

**Files:**
- Modify: `src/DainnUser.Application/ApplicationServiceExtensions.cs`

Add alongside other service registrations:

```csharp
services.AddScoped<ISessionService, SessionService>();
```

---

## Task 5: Refactor AuthenticationService to use ISessionService

**Files:**
- Modify: `src/DainnUser.Application/Services/AuthenticationService.cs`

### Change 1: Add ISessionService to constructor

Add `ISessionService _sessionService` field and constructor parameter:

```csharp
private readonly ISessionService _sessionService;

// In constructor, add parameter:
ISessionService sessionService

// Assign:
_sessionService = sessionService;
```

### Change 2: LoginAsync — session creation block (lines ~225-239)

Replace:
```csharp
if (_options.EnableSessionManagement)
{
    await _unitOfWork.Sessions.AddAsync(new UserSession
    {
        Id = sessionId,
        UserId = user.Id,
        SessionToken = refreshTokenHash,
        ...
    }, cancellationToken);
}
```

With:
```csharp
if (_options.EnableSessionManagement)
{
    await _sessionService.CreateSessionAsync(
        user.Id, refreshTokenHash, ipAddress, userAgent, cancellationToken);
}
```

### Change 3: CompleteTwoFactorLoginAsync — session creation block (lines ~321-335)

Same replacement as Change 2.

### Change 4: RefreshTokenAsync — session rotation (lines ~418-480)

Replace the session rotation and creation fallback block:
```csharp
UserSession? session = null;
if (_options.EnableSessionManagement)
{
    session = await _unitOfWork.Sessions.GetByTokenAsync(hash, cancellationToken);
}
var sessionId = session?.Id ?? Guid.NewGuid();
...
if (_options.EnableSessionManagement)
{
    if (session is not null && session.IsActive)
    {
        session.SessionToken = newRefreshTokenHash;
        ...
    }
    else
    {
        await _unitOfWork.Sessions.AddAsync(new UserSession { ... });
    }
}
```

With:
```csharp
UserSession? session = null;
var newRefreshTokenHash = _jwtTokenService.HashRefreshToken(newRefreshToken);
if (_options.EnableSessionManagement)
{
    session = await _sessionService.RotateSessionAsync(
        hash, newRefreshTokenHash, ipAddress, userAgent, cancellationToken);

    if (session is null)
    {
        session = await _sessionService.CreateSessionAsync(
            user.Id, newRefreshTokenHash, ipAddress, userAgent, cancellationToken);
    }
}
var sessionId = session?.Id ?? Guid.NewGuid();
```

Note: `newRefreshTokenHash` must be computed BEFORE the session rotation block so it's available. Move the computation up if needed.

### Change 5: RefreshTokenAsync — reuse detection (lines ~389-392)

Replace:
```csharp
if (_options.EnableSessionManagement)
{
    await _unitOfWork.Sessions.DeactivateAllByUserIdAsync(token.UserId, cancellationToken);
}
```

With:
```csharp
if (_options.EnableSessionManagement)
{
    await _sessionService.RevokeAllSessionsAsync(token.UserId, cancellationToken);
}
```

### Change 6: LogoutAsync (lines ~534-565)

Replace the entire method body's session operations with:
```csharp
public async Task LogoutAsync(Guid sessionId, CancellationToken cancellationToken = default)
{
    if (sessionId == Guid.Empty)
        return;

    var session = await _unitOfWork.Sessions.GetByIdAsync(sessionId, cancellationToken);
    if (session is null)
        return;

    // Revoke the refresh token tied to this session
    if (!string.IsNullOrWhiteSpace(session.SessionToken))
    {
        var token = await _userRepository.GetRefreshTokenByHashAsync(session.SessionToken, cancellationToken);
        if (token is not null && !token.IsUsed && !token.IsRevoked)
        {
            token.IsRevoked = true;
            token.RevokedAt = DateTime.UtcNow;
        }
    }

    await _sessionService.RevokeSessionAsync(sessionId, cancellationToken);
}
```

### Change 7: ResetPasswordAsync — session deactivation (lines ~737-739)

Replace:
```csharp
if (_options.EnableSessionManagement)
{
    await _unitOfWork.Sessions.DeactivateAllByUserIdAsync(user.Id, cancellationToken);
}
```

With:
```csharp
if (_options.EnableSessionManagement)
{
    await _sessionService.RevokeAllSessionsAsync(user.Id, cancellationToken);
}
```

### Change 8: ChangePasswordAsync — session deactivation except (lines ~784-786)

Replace:
```csharp
if (_options.EnableSessionManagement)
{
    await _unitOfWork.Sessions.DeactivateAllExceptAsync(user.Id, currentSessionId, cancellationToken);
}
```

With:
```csharp
if (_options.EnableSessionManagement)
{
    await _sessionService.RevokeAllExceptAsync(user.Id, currentSessionId, cancellationToken);
}
```

---

## Task 6: Write unit tests for SessionService

**Files:**
- Create: `tests/DainnUser.UnitTests/Services/SessionServiceTests.cs`

Test cases:
1. `CreateSessionAsync` creates a session with correct fields.
2. `CreateSessionAsync` throws `UserNotFoundException` for unknown user.
3. `CreateSessionAsync` enforces max active sessions — deactivates oldest when at limit.
4. `CreateSessionAsync` does not deactivate current session when under limit.
5. `GetActiveSessionsAsync` returns only active, non-expired sessions.
6. `RevokeSessionAsync` marks session inactive.
7. `RevokeSessionAsync` is idempotent for non-existent session.
8. `RevokeAllSessionsAsync` deactivates all user sessions.
9. `RevokeAllExceptAsync` preserves the specified session.
10. `RotateSessionAsync` updates hash and extends expiry.
11. `RotateSessionAsync` returns null when session not found.
12. `RotateSessionAsync` deactivates and returns null when session expired.
13. `UpdateLastActivityAsync` stamps `LastActivityAt`.
14. `CleanupExpiredSessionsAsync` returns count of removed sessions.

Use Moq for `ISessionRepository`, `IUserRepository`, `IUnitOfWork`, `IOptions<DainnUserOptions>`. Use FluentAssertions.

---

## Task 7: Write integration tests for SessionService

**Files:**
- Create: `tests/DainnUser.IntegrationTests/Services/SessionServiceIntegrationTests.cs`

Use existing `DatabaseFixture`. Follow pattern from `AuthenticationServiceIntegrationTests.cs`.

Test cases:
1. Full session lifecycle: create → get active → revoke → verify inactive.
2. Max sessions enforcement works end-to-end (create 6 sessions, verify only 5 active).
3. `RotateSessionAsync` works end-to-end.
4. `RevokeAllSessionsAsync` deactivates all for a user.
5. `CleanupExpiredSessionsAsync` removes expired sessions.

---

## Verification

After all tasks:
1. Run: `dotnet build` — must pass with no errors.
2. Run: `dotnet test` — all tests must pass.
3. Verify `AuthenticationServiceTests` still pass after refactoring.

---

## Spec Coverage Check

| Requirement | Task |
|---|---|
| `ISessionService` interface | Task 2 |
| `CreateSessionAsync()` | Tasks 2, 3 |
| `GetActiveSessionsAsync()` | Tasks 2, 3 |
| `RevokeSessionAsync()` | Tasks 2, 3 |
| `RevokeAllSessionsAsync()` | Tasks 2, 3 |
| `RevokeAllExceptAsync()` | Tasks 2, 3 |
| `RotateSessionAsync()` | Tasks 2, 3 |
| `UpdateLastActivityAsync()` | Tasks 2, 3 |
| `CleanupExpiredSessionsAsync()` | Tasks 2, 3 |
| Session timeout (RefreshTokenExpirationDays) | Task 3 |
| Track device info (IP, user agent) | Task 3 |
| Max active sessions / user | Tasks 1, 3 |
| Refactor AuthenticationService | Task 5 |
| DI registration | Task 4 |
| Unit tests | Task 6 |
| Integration tests | Task 7 |
