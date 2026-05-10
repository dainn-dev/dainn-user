# DainnUser — Memory

**Stack:** .NET/C# Class Library, EF Core | **DB:** Dynamic provider support | **Users:** .NET Developers

**Luôn nhớ:** Library phải flexible để custom theo business logic. Features enable/disable qua appsettings. OWASP Top 10 compliance bắt buộc.

**Mode:** CONSULTANT AGENT

---

## Current State

- Active branch: master
- Last completed: PSA-61, PSA-75, PSA-79, PSA-80, PSA-56/57/58 (sync), PSA-85, PSA-76 — all 2026-05-10. **All 11 Urgent tasks now Done.**
- Auth loop end-to-end: register → verify → login → refresh → logout. Admin: unlock-account.
- Tests: 133 unit + 54 security + 16 integration passing (1 skipped — pre-existing InMemory provider quirk). Total **203 tests**.
- Security suite: `tests/DainnUser.SecurityTests/Owasp/A0{1,2,3,4,5,7}*.cs` — adversarial tests organized by OWASP Top 10 2021 category, share `SecurityTestFixture` (in-memory db + real service wiring).
- Uncommitted — changes spanning PSA-61/75/79 still staged. Awaiting decision on commit/PR strategy.

## Key Components

Dự kiến structure:
- `DainnUser.Core/` — Domain models, interfaces
- `DainnUser.Infrastructure/` — EF Core, database implementations
- `DainnUser.Application/` — Business logic, services
- `DainnUser.Api/` — API controllers, DTOs
- `DainnUser.Web/` — Web components (Razor/Blazor)

## Features Planned

**Authentication:**
- Register / Login / Forgot Password
- 2FA (Two-Factor Authentication)
- Social Login: Google, Facebook, GitHub, Microsoft
- Session management
- Login history
- Google reCAPTCHA

**Authorization:**
- RBAC (Role-Based Access Control)

**Profile Management:**
- View/Edit profile
- Avatar upload
- Language & timezone settings
- Activity history
- Address & contact management

**Security:**
- OWASP Top 10 compliance
- Input validation
- SQL injection prevention
- XSS protection
- CSRF protection

## In Progress

(none — PSA-61/75/79 all marked Done)

## Auth flow architecture (PSA-61 / PSA-75 / PSA-79)

**JWT (PSA-61):**
- `IJwtTokenService` (Core) — `GenerateAccessToken`/`GenerateRefreshToken`/`HashRefreshToken`/`ValidateAccessToken`. Impl `JwtTokenService` (Infrastructure) uses HS256.
- `JwtOptions` (Infrastructure/Configuration) — Secret/Issuer/Audience. Config path `DainnUser:Jwt:*`. Secret must be ≥ 32 bytes (validated at startup).
- `LoginDto` / `RefreshTokenDto` (Application) input. `LoginResult` / `AuthenticatedUserInfo` (Core/Models/Authentication) output.
- Domain exceptions (`Core/Exceptions`): `InvalidCredentialsException` (401, generic — no enumeration), `EmailNotVerifiedException` (403), `AccountLockedException` (423), `AccountInactiveException` (403), `InvalidRefreshTokenException` (401, with `IsReuseDetected` flag).
- Refresh tokens stored hashed (SHA-256). `UserSession.SessionToken` shares the same hash so refresh resolves session+token in one lookup.
- API wires bearer via `services.AddDainnUserJwtAuthentication(configuration)` (DainnUser.Api.Extensions). Pipeline order: `UseAuthentication()` → `UseAuthorization()` → `UseDainnUser()`.

**Repository pattern for tokens:**
- `IUserRepository.AddTokenAsync` adds `UserToken` via DbSet (NOT via `User.Tokens` navigation — InMemory provider mishandles that). Apply this pattern for any future flow that adds a token to a tracked existing user.
- `IUserRepository.GetRefreshTokenByHashAsync` returns the row regardless of state — caller checks `IsUsed`/`IsRevoked`/`ExpiresAt`.
- `IUserRepository.RevokeAllRefreshTokensAsync` is the incident-response hook for token reuse.

**Account lockout (PSA-76):**
- Lockout fires when `FailedLoginAttempts >= MaxFailedLoginAttempts` after a wrong-password attempt; auto-clears once `LockoutEnd <= now` (no scheduled job needed).
- Email notification on the lock-triggering attempt only — wrapped in try/catch so SMTP outage never blocks login flow. No spam during active lockout because `LoginAsync` short-circuits with `AccountLockedException` before reaching the password verify path.
- Admin unlock: `IAuthenticationService.UnlockAccountAsync(userId)` resets counters + clears `LockoutEnd` + restores `Status.Locked → Active` (does NOT touch Suspended/Deactivated — those are separate admin decisions).
- Endpoint `POST /api/auth/admin/unlock-account/{userId:guid}` requires `[Authorize(Roles = "Administrator")]`. Default seeded role is `Administrator` (DbSeeder.cs role with id `00000000-0000-0000-0000-000000000001`).
- Rate limit rule for `/api/auth/admin/*` is 10/60s PerUser.

**Logout (PSA-80):**
- `IAuthenticationService.LogoutAsync(sessionId)` is idempotent — empty/unknown sessionId is no-op.
- Deactivates session AND revokes the linked refresh token (only when not already used; preserves audit trail).
- Endpoint `POST /api/auth/logout` requires `[Authorize]`; reads `sid` claim from JWT to identify the session.
- All other auth endpoints are explicitly `[AllowAnonymous]` for defensive clarity (so a future controller-level `[Authorize]` won't break public flows).

**Refresh rotation (PSA-79):**
- One-time-use: each refresh marks the presented token `IsUsed=true`, mints a new one. Same `sessionId` preserved across rotations (rotates `UserSession.SessionToken` to the new hash in-place).
- Reuse detection: presenting an already-used token throws `InvalidRefreshTokenException(IsReuseDetected=true)` AND revokes all of the user's active refresh tokens + deactivates sessions.
- Defensive: if session row is missing/inactive when refreshing a still-valid token, a new session is created rather than failing.

**Rate limiting (PSA-75):**
- `RateLimitingOptions` config path `DainnUser:RateLimiting:{Enabled, Rules[], WhitelistIps[]}`. Rules: `Endpoint` (exact or `prefix*`), `MaxRequests`, `WindowSeconds`, `Mode` (PerIp / PerUser / PerIpAndUser), `SegmentsPerWindow`.
- Master switch: `DainnUserOptions.EnableRateLimiting` AND `RateLimitingOptions.Enabled` must both be true.
- Implementation: `RateLimiterRegistry` (singleton, holds `PartitionedRateLimiter<string>` per rule using `SlidingWindowRateLimiter`) + `DainnUserRateLimitingMiddleware` (registered via `UseDainnUser()`).
- 429 response body: `{success:false,message:"Too many requests. Please retry later."}` + `Retry-After` header (seconds).
- IP resolution honors `X-Forwarded-For` (consumers behind a proxy must also wire `UseForwardedHeaders`).
- In-memory only — for cluster deployments, either run rate limiting at the edge (nginx/Cloudflare) or replace the registry with a distributed implementation.

## Recent Decisions

Xem `.claude/memory/decisions.md`

---

_Keep this file under 200 lines. Archive old context with compress-context skill._
