# Architectural Decisions

_Thêm decisions vào đây khi chúng được đưa ra._

---

## Decision: Use Clean Architecture / Onion Architecture

**Date:** 2026-05-08
**Decision:** Organize codebase theo Clean Architecture với 4 layers: Core, Infrastructure, Application, Presentation
**Reason:** 
- Separation of concerns rõ ràng
- Core domain không phụ thuộc vào infrastructure
- Dễ test (mock infrastructure layer)
- Dễ thay đổi database provider hoặc UI framework
**Alternatives considered:** N-tier architecture (quá coupled), modular monolith (overkill cho library)

---

## Decision: Use Entity Framework Core với Multiple Provider Support

**Date:** 2026-05-08
**Decision:** Dùng EF Core làm ORM, hỗ trợ SQL Server, PostgreSQL, MySQL, SQLite
**Reason:**
- User yêu cầu dynamic database provider
- EF Core có provider pattern built-in
- Code-first migrations dễ quản lý
- Strongly-typed queries, LINQ support
**Alternatives considered:** Dapper (quá low-level cho library), ADO.NET (không có abstraction)

---

## Decision: Distribute as NuGet Package với Multiple Sub-Packages

**Date:** 2026-05-08
**Decision:** Tạo meta-package `DainnUser` và các sub-packages cho từng layer
**Reason:**
- Developers có thể chọn install chỉ những gì cần (e.g., chỉ Core + Infrastructure nếu không dùng API)
- Giảm dependencies không cần thiết
- Dễ versioning từng component
**Alternatives considered:** Single monolithic package (quá nặng, nhiều unused dependencies)

---

## Decision: Use ASP.NET Core Identity as Foundation

**Date:** 2026-05-08
**Decision:** Extend ASP.NET Core Identity thay vì build authentication từ đầu
**Reason:**
- Battle-tested, secure by default
- Hỗ trợ sẵn password hashing, user management, roles
- Dễ integrate với OAuth providers
- Microsoft maintained
**Alternatives considered:** Custom authentication (reinvent the wheel, security risks)

---

## Decision: Options Pattern for Configuration

**Date:** 2026-05-08
**Decision:** Dùng IOptions<T> pattern cho tất cả configuration
**Reason:**
- User yêu cầu enable/disable features qua appsettings.json
- Strongly-typed configuration
- Validation on startup
- Testable
**Alternatives considered:** Static configuration class (không testable), direct IConfiguration injection (không type-safe)

---

## Decision: FluentValidation for Complex Validation

**Date:** 2026-05-08
**Decision:** Dùng FluentValidation cho business logic validation
**Reason:**
- Readable, maintainable validation rules
- Reusable validators
- Async validation support
- Better error messages
**Alternatives considered:** Data Annotations (limited, không reusable), manual validation (verbose)

---

## Decision: JWT for API Authentication

**Date:** 2026-05-08
**Decision:** Dùng JWT tokens cho API authentication
**Reason:**
- Stateless authentication
- Works well với mobile/SPA clients
- Standard, widely supported
- Refresh token pattern cho security
**Alternatives considered:** Session-based (không scale với distributed systems), API keys (ít flexible)

---

## Decision: JWT Login Issues Hashed Refresh Token + Tied Session

**Date:** 2026-05-10
**Decision:** On `LoginAsync`, generate a random refresh token, store its SHA-256 hash in `UserToken.TokenValue` (TokenType=RefreshToken) AND in `UserSession.SessionToken` for the same session. Plain refresh token returned to client only at login.
**Reason:**
- Hashing refresh tokens at rest mirrors password storage practice; DB compromise doesn't yield usable tokens.
- Sharing the hash between `UserToken` and `UserSession` means the future refresh endpoint (PSA-79) resolves both rows in a single index lookup without a separate FK column.
- Plain token never persisted, only signed JWT-equivalent cryptographic material in transit.
**Alternatives considered:** Storing plain refresh tokens (rejected — DB-leak risk), JWT-as-refresh-token (rejected — can't be revoked without a denylist).

---

## Decision: Add UserToken via DbSet, Not Navigation Collection

**Date:** 2026-05-10
**Decision:** `IUserRepository.AddTokenAsync(UserToken)` adds via `_context.Set<UserToken>().AddAsync` rather than mutating `User.Tokens`.
**Reason:**
- EF Core InMemory provider produces `DbUpdateConcurrencyException` when adding to a navigation collection on an already-tracked entity (the success path of `LoginAsync` triggered this). Same root cause as the pre-existing skipped test `ResendVerificationEmail_EndToEnd_RevokesOldTokens`.
- Setting `UserId` on the new `UserToken` and adding via DbSet establishes the FK without touching navigation; works consistently across InMemory + relational providers.
**How to apply:** Any future flow that adds a token to an EXISTING tracked user must use `AddTokenAsync`. `RegisterAsync` keeps using `user.Tokens.Add` because the user itself is new (Added state) and EF cascades correctly.

---

## Decision: Generic Error on Invalid Credentials (No User Enumeration)

**Date:** 2026-05-10
**Decision:** Login returns the same generic "Invalid email or password" / 401 when (a) the email doesn't exist OR (b) the password is wrong. `EmailNotVerifiedException` (403), `AccountLockedException` (423), and `AccountInactiveException` (403) are distinguishable because they only fire AFTER password verification succeeds — they don't leak existence to unauthenticated probes.
**Reason:** OWASP A07 (Authentication Failures) — distinguishing "no such email" from "wrong password" lets attackers enumerate accounts. Status-leak after correct password is a usability tradeoff acknowledged in PSA-61's threat model.

---

## Decision: Sliding-Window Rate Limiting via System.Threading.RateLimiting (PSA-75)

**Date:** 2026-05-10
**Decision:** Use the BCL `System.Threading.RateLimiting.PartitionedRateLimiter<string>` with `SlidingWindowRateLimiter` per rule, with custom DainnUser middleware on top to handle path matching, IP/user partitioning, whitelisting, and the 429 response body. Built-in ASP.NET Core `AddRateLimiter()` was rejected because its named policies can't be added from config dictionaries at startup.
**Reason:**
- No extra NuGet beyond `System.Threading.RateLimiting` (already net8.0-friendly).
- Sliding window gives smoother behavior than fixed window for brute-force scenarios.
- Custom middleware lets us bind config purely from `DainnUser:RateLimiting:Rules[]` without code changes per rule.
**Trade-off / how to apply:** State is in-memory. Acceptable for single-node or sticky-session clusters. For horizontally scaled deployments, consumers should layer rate limiting at the edge (nginx/Cloudflare) or replace the registry. Document this loud and clear when shipping the NuGet.

---

## Decision: Refresh Token Rotation with Reuse Detection (PSA-79)

**Date:** 2026-05-10
**Decision:** Refresh tokens are one-time-use: each successful refresh marks the presented token `IsUsed=true` and issues a new one. The session row is rotated in-place (same `sessionId`) so the JWT `sid` claim stays continuous. Replaying an already-used token triggers `InvalidRefreshTokenException(IsReuseDetected=true)` AND revokes all of the user's active refresh tokens + deactivates all sessions.
**Reason:**
- Token rotation is industry-standard (RFC 6819) — limits the blast radius of a leaked token.
- Reuse detection assumes leak/theft when both the legitimate client and an attacker hold the token; revoking everything forces re-authentication, which the legitimate user can do but the attacker cannot (without password).
- Preserving `sessionId` across rotation keeps audit trails / session-management UIs coherent.
**Trade-off:** Concurrent refresh from multiple processes (e.g., the legitimate client's parallel tabs) is treated as reuse. Acceptable for now — clients should serialize refreshes. If this becomes a real pain point, consider a short grace window where the just-rotated token still resolves to its successor.

---

## Decision: OWASP Top 10 Compliance as Core Requirement

**Date:** 2026-05-08
**Decision:** Implement security controls cho OWASP Top 10 threats
**Reason:**
- User yêu cầu explicit
- Professional library cần security standards
- Builds trust với developers
**Implementation:**
- A01 Broken Access Control → RBAC, authorization policies
- A02 Cryptographic Failures → Data Protection API, secure password hashing
- A03 Injection → Parameterized queries (EF Core), input validation
- A04 Insecure Design → Secure by default configuration
- A05 Security Misconfiguration → Validation on startup, secure defaults
- A06 Vulnerable Components → Keep dependencies updated
- A07 Authentication Failures → 2FA, rate limiting, login history
- A08 Software/Data Integrity → Signed packages, integrity checks
- A09 Logging Failures → Structured logging, audit trail
- A10 SSRF → Input validation, whitelist external calls

---
