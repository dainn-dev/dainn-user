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
